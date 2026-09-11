using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Services
{
    public class AdminManagementService : IAdminManagementService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminManagementService> _logger;

        public AdminManagementService(AppDbContext context, ILogger<AdminManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all admins by joining the Admins and Users tables.
        /// </summary>
        public async Task<List<AdminResponseDto>> GetAllAdminsAsync()
        {
            var admins = await _context.Admins
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return admins.Select(a => new AdminResponseDto
            {
                AdminId = a.AdminId,
                UserId = a.UserId,
                FullName = a.User?.FullName ?? "Unknown",
                Email = a.User?.Email ?? "Unknown",
                AccessLevel = a.AccessLevel,
                SecurePin = a.SecurePin,
                IsActive = a.User?.IsActive ?? false,
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        /// <summary>
        /// Creates a new admin:
        /// 1. Validates email is not already taken
        /// 2. Finds the ADMIN role
        /// 3. Creates a User record with BCrypt-hashed password
        /// 4. Creates an Admin record with a random 4-digit PIN
        /// </summary>
        public async Task<AdminResponseDto> CreateAdminAsync(CreateAdminRequestDto request)
        {
            // 1. Check for duplicate email
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                throw new ArgumentException($"A user with email '{request.Email}' already exists.");
            }

            // 2. Find the ADMIN role
            var adminRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "ADMIN" || r.RoleName == "Admin");

            if (adminRole == null)
            {
                throw new InvalidOperationException("ADMIN role not found in the database. Please seed roles first.");
            }

            // 3. Generate a random 4-digit secure PIN (1000 - 9999)
            var random = new Random();
            var securePin = random.Next(1000, 10000).ToString();

            // 4. Create User record
            var newUser = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = adminRole.RoleId,
                IsActive = true
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // 5. Create Admin record linked to the new user
            var newAdmin = new Admin
            {
                UserId = newUser.UserId,
                AccessLevel = "Admin",
                SecurePin = securePin
            };

            _context.Admins.Add(newAdmin);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "AdminManagement: Created new admin '{FullName}' ({Email}) with AdminId {AdminId}, PIN {Pin}",
                newUser.FullName, newUser.Email, newAdmin.AdminId, securePin);

            return new AdminResponseDto
            {
                AdminId = newAdmin.AdminId,
                UserId = newUser.UserId,
                FullName = newUser.FullName,
                Email = newUser.Email,
                AccessLevel = newAdmin.AccessLevel,
                SecurePin = securePin,
                IsActive = newUser.IsActive,
                CreatedAt = newAdmin.CreatedAt
            };
        }
    }
}
