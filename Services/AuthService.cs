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
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // ── 1. Find user by email (include the Role navigation) ──
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                _logger.LogWarning("Login failed: no user found for email {Email}", request.Email);
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // ── 2. Verify the account is active ──
            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: account is deactivated for UserId {UserId}", user.UserId);
                throw new UnauthorizedAccessException("Your account has been deactivated. Please contact support.");
            }

            // ── 3. Verify password hash ──
            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            catch (BCrypt.Net.SaltParseException ex)
            {
                _logger.LogError(ex, "Password verification failed due to invalid salt format for UserId {UserId}. Resetting password required.", user.UserId);
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed: incorrect password for UserId {UserId}", user.UserId);
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // ── 4. Resolve the role name from the Role table ──
            var roleName = user.Role?.RoleName ?? "Unknown";

            // ── 5. Admin-specific validation ──
            if (request.IsAdmin)
            {
                // The user must actually have an Admin role
                if (!roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                    && !roleName.Equals("ADMIN", StringComparison.OrdinalIgnoreCase)
                    && !roleName.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Login failed: user {UserId} toggled Admin but role is {Role}", user.UserId, roleName);
                    throw new UnauthorizedAccessException("You do not have administrator privileges.");
                }

                // PIN is mandatory for admin login
                if (string.IsNullOrWhiteSpace(request.Pin) || request.Pin.Length != 4)
                {
                    throw new ArgumentException("A valid 4-digit admin PIN is required.");
                }

                // Verify the PIN against Admins table SecurePinHash (BCrypt)
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == user.UserId);
                if (admin == null || string.IsNullOrEmpty(admin.SecurePinHash))
                {
                    _logger.LogWarning("Login failed: no Admin record or SecurePinHash found for UserId {UserId}", user.UserId);
                    throw new UnauthorizedAccessException("Admin credentials not configured properly.");
                }

                bool isPinValid = false;
                try
                {
                    isPinValid = BCrypt.Net.BCrypt.Verify(request.Pin, admin.SecurePinHash);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AuthService: BCrypt verify error for Admin UserId {UserId}", user.UserId);
                }

                if (!isPinValid)
                {
                    _logger.LogWarning("Login failed: incorrect PIN for Admin UserId {UserId}", user.UserId);
                    throw new UnauthorizedAccessException("Incorrect admin PIN.");
                }

                if (!isPinValid)
                {
                    _logger.LogWarning("Login failed: incorrect PIN for Admin UserId {UserId}", user.UserId);
                    throw new UnauthorizedAccessException("Incorrect admin PIN.");
                }
            }
            else
            {
                // Vendor login — ensure the user actually has a Vendor role
                if (!roleName.Equals("Vendor", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Login failed: user {UserId} attempted vendor login but role is {Role}", user.UserId, roleName);
                    throw new UnauthorizedAccessException("Invalid credentials for vendor login.");
                }
            }

            // ── 6. Generate JWT token ──
            var token = GenerateJwtToken(user, roleName);

            _logger.LogInformation("User {UserId} ({Role}) logged in successfully", user.UserId, roleName);

            return new LoginResponseDto
            {
                Token = token,
                Role = roleName,
                FullName = user.FullName,
                Email = user.Email,
                UserId = user.UserId
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
