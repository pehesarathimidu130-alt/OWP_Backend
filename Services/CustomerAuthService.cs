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

        public CustomerAuthService(AppDbContext context, IConfiguration configuration, ILogger<CustomerAuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
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
                Email = user.Email
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
                Email = user.Email
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
    }
}
