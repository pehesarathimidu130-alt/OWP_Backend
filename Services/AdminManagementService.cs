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

        /// <summary>
        /// Verifies the submitted PIN against the stored plain-text SecurePin for this admin.
        /// SecurePin is stored as a plain 4-digit string (see Admin.cs) — no hashing is used.
        /// </summary>
        public async Task<bool> VerifyPinAsync(int userId, string pin)
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (admin == null)
            {
                _logger.LogWarning("VerifyPin: no Admin record found for UserId {UserId}", userId);
                return false;
            }

            // Direct string comparison — SecurePin is stored unhashed (matches AuthService.cs line 92)
            return admin.SecurePin == pin;
        }

        /// <summary>
        /// Generates a unique random 4-digit PIN candidate for the given admin.
        /// Does NOT save to the database — the caller must call SaveNewPinAsync to commit.
        /// Returns AlreadyInUse = true if all attempts collide (extremely unlikely).
        /// </summary>
        public async Task<GeneratePinResponseDto> GenerateCandidatePinAsync(int userId)
        {
            // Verify the admin exists
            var adminExists = await _context.Admins.AnyAsync(a => a.UserId == userId);
            if (!adminExists)
                throw new InvalidOperationException($"Admin record not found for UserId {userId}.");

            // Collect every PIN currently in use across ALL admins
            var allUsedPins = await _context.Admins
                .Select(a => a.SecurePin)
                .ToListAsync();

            var random = new Random();
            const int maxAttempts = 20;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 4-digit PIN in range 1000–9999 (never starts with 0)
                var candidate = random.Next(1000, 10000).ToString();

                if (allUsedPins.Contains(candidate))
                    continue; // collision — try again

                _logger.LogInformation(
                    "GenerateCandidate: unique PIN found for UserId {UserId} on attempt {Attempt}",
                    userId, attempt + 1);

                // Return the candidate — NOT saved yet
                return new GeneratePinResponseDto { Pin = candidate, AlreadyInUse = false };
            }

            // Extremely unlikely — all 20 attempts collided
            _logger.LogWarning(
                "GenerateCandidate: all {Max} attempts for UserId {UserId} produced collisions",
                maxAttempts, userId);

            return new GeneratePinResponseDto { Pin = string.Empty, AlreadyInUse = true };
        }

        /// <summary>
        /// Persists a new PIN for the admin after the user confirms it.
        /// Re-verifies the current PIN before saving to prevent CSRF-style substitution.
        /// Returns false if currentPin is wrong; throws if the admin record is not found.
        /// </summary>
        public async Task<bool> SaveNewPinAsync(int userId, string currentPin, string newPin)
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (admin == null)
                throw new InvalidOperationException($"Admin record not found for UserId {userId}.");

            // Re-confirm the caller's identity with the original PIN they entered at the start
            if (admin.SecurePin != currentPin)
            {
                _logger.LogWarning("SaveNewPin: PIN re-verification failed for UserId {UserId}", userId);
                return false;
            }

            admin.SecurePin = newPin;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "SaveNewPin: PIN updated successfully for UserId {UserId}", userId);

            return true;
        }

        /// <summary>
        /// Verifies the current password using BCrypt and updates with the new hashed password.
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                _logger.LogWarning("ChangePassword: user not found for UserId {UserId}", userId);
                return false;
            }

            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChangePassword: BCrypt verify error for UserId {UserId}", userId);
                return false;
            }

            if (!isPasswordValid)
            {
                _logger.LogWarning("ChangePassword: wrong current password for UserId {UserId}", userId);
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("ChangePassword: password successfully updated for UserId {UserId}", userId);
            return true;
        }

        /// <summary>
        /// Updates the full name for the given user.
        /// </summary>
        public async Task<bool> UpdateProfileAsync(int userId, string fullName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                _logger.LogWarning("UpdateProfile: user not found for UserId {UserId}", userId);
                return false;
            }

            user.FullName = fullName.Trim();
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("UpdateProfile: profile updated for UserId {UserId}", userId);
            return true;
        }
    }
}

