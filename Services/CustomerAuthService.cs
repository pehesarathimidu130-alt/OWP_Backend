using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Backend.Services
{
    public class CustomerAuthService : ICustomerAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CustomerAuthService> _logger;
        private readonly IEmailService _emailService;
        private readonly IGoogleTokenVerifier _googleTokenVerifier;

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string Code, DateTime ExpiresAt)> _resetTokens = new();

        public CustomerAuthService(AppDbContext context, IConfiguration configuration, ILogger<CustomerAuthService> logger, IEmailService emailService, IGoogleTokenVerifier googleTokenVerifier)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
            _googleTokenVerifier = googleTokenVerifier;
        }

        public async Task<CustomerAuthResponseDto> RegisterAsync(CustomerRegisterRequestDto request)
        {
            var email = request.Email.Trim().ToLower();

            // 1. Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
            {
                _logger.LogWarning("Registration failed: email {Email} already exists", email);
                throw new InvalidOperationException("A user with this email already exists.");
            }

            // 2. Hash Password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 3. Get or Create Customer Role
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
            if (customerRole == null)
            {
                customerRole = new Role { RoleName = "Customer" };
                _context.Roles.Add(customerRole);
                await _context.SaveChangesAsync();
            }

            // 4. Create User Record
            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = passwordHash,
                PhoneNumber = request.Phone?.Trim(),
                RoleId = customerRole.RoleId,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 5. Create Customer Record
            // Split FullName to populate FirstName and LastName
            var nameParts = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.Length > 0 ? nameParts[0] : "Unknown";
            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "Unknown";

            var customer = new Customer
            {
                UserId = user.UserId,
                FirstName = firstName,
                LastName = lastName
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // 6. Generate Token
            var token = GenerateJwtToken(user, "Customer");

            _logger.LogInformation("Customer registered successfully with UserId {UserId}, CustomerId {CustomerId}", user.UserId, customer.CustomerId);

            return new CustomerAuthResponseDto
            {
                Token = token,
                CustomerId = customer.CustomerId,
                Role = "Customer",
                FullName = user.FullName,
                Email = user.Email,
                ProfilePhotoUrl = customer.ProfilePhotoUrl
            };
        }

        public async Task<CustomerAuthResponseDto> LoginAsync(CustomerLoginRequestDto request)
        {
            var email = request.Email.Trim().ToLower();

            // 1. Find User
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                _logger.LogWarning("Login failed: no user found for email {Email}", email);
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: account deactivated for UserId {UserId}", user.UserId);
                throw new UnauthorizedAccessException("Your account has been deactivated.");
            }

            // 2. Verify Password
            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password verification failed for UserId {UserId}", user.UserId);
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed: incorrect password for UserId {UserId}", user.UserId);
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // 3. Verify Role
            if (!user.Role?.RoleName.Equals("Customer", StringComparison.OrdinalIgnoreCase) ?? true)
            {
                _logger.LogWarning("Login failed: user {UserId} is not a Customer", user.UserId);
                throw new UnauthorizedAccessException("Invalid credentials for customer login.");
            }

            // 4. Get Customer Record
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.UserId);
            if (customer == null)
            {
                _logger.LogWarning("Login failed: missing Customer record for UserId {UserId}", user.UserId);
                throw new UnauthorizedAccessException("Customer profile not found.");
            }

            // 5. Generate Token
            var token = GenerateJwtToken(user, "Customer");

            _logger.LogInformation("Customer {CustomerId} logged in successfully", customer.CustomerId);

            return new CustomerAuthResponseDto
            {
                Token = token,
                CustomerId = customer.CustomerId,
                Role = "Customer",
                FullName = user.FullName,
                Email = user.Email,
                ProfilePhotoUrl = customer.ProfilePhotoUrl
            };
        }

        public async Task<CustomerAuthResponseDto> GoogleLoginAsync(GoogleSignInRequest request)
        {
            // 1. Verify token
            var googleResult = await _googleTokenVerifier.VerifyAsync(request.IdToken);

            // 2. Look up by external login (Provider=Google, ProviderSubject=sub)
            var externalLogin = await _context.UserExternalLogins
                .Include(uel => uel.User)
                    .ThenInclude(u => u!.Role)
                .FirstOrDefaultAsync(uel => uel.Provider == "Google" && uel.ProviderSubject == googleResult.Sub);

            User? user;

            if (externalLogin != null)
            {
                user = externalLogin.User!;
                if (!user.IsActive)
                {
                    _logger.LogWarning("Google login failed: account deactivated for UserId {UserId}", user.UserId);
                    throw new UnauthorizedAccessException("Your account has been deactivated.");
                }
            }
            else
            {
                // 3. Fall back to email lookup
                var emailLower = googleResult.Email.ToLower();
                user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);

                if (user != null)
                {
                    // Check if role is Customer
                    if (!user.Role?.RoleName.Equals("Customer", StringComparison.OrdinalIgnoreCase) ?? true)
                    {
                        _logger.LogWarning("Google login denied for UserId {UserId}: role is {Role}, not Customer", user.UserId, user.Role?.RoleName);
                        throw new UnauthorizedAccessException("This email is registered under a different account type.");
                    }

                    if (!user.IsActive)
                    {
                        _logger.LogWarning("Google login failed: account deactivated for UserId {UserId}", user.UserId);
                        throw new UnauthorizedAccessException("Your account has been deactivated.");
                    }

                    // Link the Google sub to this existing Customer user
                    _context.UserExternalLogins.Add(new UserExternalLogin
                    {
                        UserId = user.UserId,
                        Provider = "Google",
                        ProviderSubject = googleResult.Sub
                    });
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Linked Google sub to existing Customer UserId={UserId}", user.UserId);
                }
                else
                {
                    // 4. Create new customer user
                    var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
                    if (customerRole == null)
                    {
                        customerRole = new Role { RoleName = "Customer" };
                        _context.Roles.Add(customerRole);
                        await _context.SaveChangesAsync();
                    }

                    user = new User
                    {
                        FullName = googleResult.FullName,
                        Email = googleResult.Email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                        PhoneNumber = null,
                        RoleId = customerRole.RoleId,
                        IsActive = true
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    var nameParts = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var firstName = nameParts.Length > 0 ? nameParts[0] : "Unknown";
                    var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "Unknown";

                    var customer = new Customer
                    {
                        UserId = user.UserId,
                        FirstName = firstName,
                        LastName = lastName,
                        ProfilePhotoUrl = googleResult.Picture
                    };

                    _context.Customers.Add(customer);

                    _context.UserExternalLogins.Add(new UserExternalLogin
                    {
                        UserId = user.UserId,
                        Provider = "Google",
                        ProviderSubject = googleResult.Sub
                    });

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created new Customer via Google sign-in UserId={UserId}", user.UserId);
                }
            }

            var customerRecord = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user!.UserId);
            if (customerRecord == null)
            {
                throw new UnauthorizedAccessException("Customer profile not found.");
            }

            // Sync Google profile picture if customer doesn't have one set yet
            if (string.IsNullOrWhiteSpace(customerRecord.ProfilePhotoUrl) && !string.IsNullOrWhiteSpace(googleResult.Picture))
            {
                customerRecord.ProfilePhotoUrl = googleResult.Picture;
                await _context.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user!, "Customer");

            return new CustomerAuthResponseDto
            {
                Token = token,
                CustomerId = customerRecord.CustomerId,
                Role = "Customer",
                FullName = user!.FullName,
                Email = user.Email,
                ProfilePhotoUrl = customerRecord.ProfilePhotoUrl
            };
        }

        private string GenerateJwtToken(User user, string roleName)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings.GetValue<string>("SecretKey")
                ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, roleName)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings.GetValue<string>("Issuer"),
                audience: jwtSettings.GetValue<string>("Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string?> ForgotPasswordAsync(CustomerForgotPasswordRequestDto request)
        {
            var email = request.Email.Trim().ToLower();

            // Verify user exists
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent email {Email}", email);
                throw new InvalidOperationException("No registered account found with this email address.");
            }

            // Generate secure 6-digit verification code
            var code = Random.Shared.Next(100000, 999999).ToString();
            _resetTokens[email] = (code, DateTime.UtcNow.AddMinutes(15));

            _logger.LogInformation("Generated password reset code for {Email}", email);

            // Send via EmailService
            await _emailService.SendPasswordResetEmailAsync(user.Email, code);

            // If SMTP credentials are not yet configured, return code as debugCode for local development/testing
            var smtpUser = _configuration["SmtpSettings:Username"];
            var smtpPass = _configuration["SmtpSettings:Password"];
            var isSmtpConfigured = !string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPass);

            return isSmtpConfigured ? null : code;
        }

        public async Task<bool> ResetPasswordAsync(CustomerResetPasswordRequestDto request)
        {
            var email = request.Email.Trim().ToLower();
            var code = request.Token.Trim();

            // Check if token exists and is valid
            if (!_resetTokens.TryGetValue(email, out var entry))
            {
                _logger.LogWarning("Password reset failed: no active code for {Email}", email);
                throw new InvalidOperationException("Verification code has expired or was not requested. Please request a new code.");
            }

            if (entry.ExpiresAt < DateTime.UtcNow)
            {
                _resetTokens.TryRemove(email, out _);
                _logger.LogWarning("Password reset failed: code expired for {Email}", email);
                throw new InvalidOperationException("Verification code has expired. Please request a new code.");
            }

            if (entry.Code != code)
            {
                _logger.LogWarning("Password reset failed: incorrect code for {Email}", email);
                throw new InvalidOperationException("Invalid verification code. Please check your email and try again.");
            }

            // Find user and update password
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null)
            {
                throw new InvalidOperationException("User account not found.");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordHash = passwordHash;
            await _context.SaveChangesAsync();

            // Consume token
            _resetTokens.TryRemove(email, out _);

            _logger.LogInformation("Password successfully reset for UserId {UserId} ({Email})", user.UserId, email);

            return true;
        }
    }
}
