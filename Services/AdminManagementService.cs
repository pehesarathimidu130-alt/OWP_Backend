using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

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

        // =========================================================================
        // SUPER ADMIN ADMINISTRATOR CRUD (Consolidated in Admins Table)
        // =========================================================================

        /// <summary>
        /// Queries administrators from the Admins table joined with Users.
        /// Does NOT expose plaintext PINs or hashes.
        /// </summary>
        public async Task<List<AdminListItemDto>> GetAdministratorsAsync(string? search = null, string? role = null, string? status = null)
        {
            var query = _context.Admins
                .Include(a => a.User)
                .AsQueryable();

            // Search filter (name, email, or phone)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(a =>
                    (a.FirstName != null && a.FirstName.ToLower().Contains(term)) ||
                    (a.LastName != null && a.LastName.ToLower().Contains(term)) ||
                    (a.PhoneNumber != null && a.PhoneNumber.ToLower().Contains(term)) ||
                    (a.User != null && (
                        a.User.FullName.ToLower().Contains(term) ||
                        a.User.Email.ToLower().Contains(term) ||
                        (a.User.PhoneNumber != null && a.User.PhoneNumber.ToLower().Contains(term))
                    ))
                );
            }

            // Role filter ("Admin" or "SuperAdmin")
            if (!string.IsNullOrWhiteSpace(role) && !role.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var roleTerm = role.Trim().ToLower();
                if (roleTerm.Contains("super"))
                {
                    query = query.Where(a => a.AccessLevel.ToLower() == "superadmin");
                }
                else
                {
                    query = query.Where(a => a.AccessLevel.ToLower() == "admin");
                }
            }

            // Status filter ("Active" or "Inactive")
            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(a => a.User != null && a.User.IsActive);
                }
                else if (status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(a => a.User != null && !a.User.IsActive);
                }
            }

            var admins = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return admins.Select(a =>
            {
                var resolvedName = !string.IsNullOrWhiteSpace(a.FullName) ? a.FullName : (a.User?.FullName ?? "Admin");
                var nameParts = resolvedName.Split(' ', 2);
                var fName = !string.IsNullOrWhiteSpace(a.FirstName) ? a.FirstName : nameParts[0];
                var lName = !string.IsNullOrWhiteSpace(a.LastName) ? a.LastName : (nameParts.Length > 1 ? nameParts[1] : "");

                return new AdminListItemDto
                {
                    AdminId = a.AdminId,
                    UserId = a.UserId,
                    FirstName = fName,
                    LastName = lName,
                    FullName = $"{fName} {lName}".Trim(),
                    Email = a.User?.Email ?? "Unknown",
                    PhoneNumber = a.PhoneNumber ?? a.User?.PhoneNumber,
                    AccessLevel = a.AccessLevel,
                    SecurePin = "••••", // Strictly masked — no plaintext in DB or response
                    HasPinConfigured = !string.IsNullOrEmpty(a.SecurePinHash),
                    Department = a.Department ?? "Administration",
                    IsActive = a.User?.IsActive ?? true,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                };
            }).ToList();
        }

        /// <summary>
        /// Retrieves a single administrator by AdminId or UserId.
        /// </summary>
        public async Task<AdminListItemDto?> GetAdministratorByIdAsync(int id)
        {
            var admin = await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AdminId == id || a.UserId == id);

            if (admin == null) return null;

            var resolvedName = !string.IsNullOrWhiteSpace(admin.FullName) ? admin.FullName : (admin.User?.FullName ?? "Admin");
            var nameParts = resolvedName.Split(' ', 2);
            var fName = !string.IsNullOrWhiteSpace(admin.FirstName) ? admin.FirstName : nameParts[0];
            var lName = !string.IsNullOrWhiteSpace(admin.LastName) ? admin.LastName : (nameParts.Length > 1 ? nameParts[1] : "");

            return new AdminListItemDto
            {
                AdminId = admin.AdminId,
                UserId = admin.UserId,
                FirstName = fName,
                LastName = lName,
                FullName = $"{fName} {lName}".Trim(),
                Email = admin.User?.Email ?? "Unknown",
                PhoneNumber = admin.PhoneNumber ?? admin.User?.PhoneNumber,
                AccessLevel = admin.AccessLevel,
                SecurePin = "••••",
                HasPinConfigured = !string.IsNullOrEmpty(admin.SecurePinHash),
                Department = admin.Department ?? "Administration",
                IsActive = admin.User?.IsActive ?? true,
                CreatedAt = admin.CreatedAt,
                UpdatedAt = admin.UpdatedAt
            };
        }

        /// <summary>
        /// Creates a new administrator in Admins & Users with auto-generated 4-digit PIN and BCrypt hashing.
        /// Returns the raw 4-digit PIN ONLY once in this response.
        /// </summary>
        public async Task<CreateAdminResponseDto> CreateAdministratorAsync(CreateAdminRequestDto request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            // 1. Check for duplicate email
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (existingUser != null)
            {
                throw new ArgumentException($"A user with email '{request.Email}' already exists.");
            }

            // 2. Resolve Role
            var isSuperAdmin = request.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                               request.Role.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase);

            var targetRoleName = isSuperAdmin ? "SUPER_ADMIN" : "ADMIN";
            var role = await _context.Roles.FirstOrDefaultAsync(r =>
                r.RoleName == targetRoleName ||
                r.RoleName == (isSuperAdmin ? "SuperAdmin" : "Admin"));

            if (role == null)
            {
                role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "ADMIN" || r.RoleName == "SUPER_ADMIN");
                if (role == null)
                    throw new InvalidOperationException("Admin roles not found in the database. Please seed roles first.");
            }

            // 3. Cryptographically generate a random 4-digit PIN (1000 - 9999)
            var rawPin = RandomNumberGenerator.GetInt32(1000, 10000).ToString();

            // 4. Hash password and PIN strictly with BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var pinHash = BCrypt.Net.BCrypt.HashPassword(rawPin);

            // 5. Persist User and Admin within a database transaction
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var fullName = $"{request.FirstName} {request.LastName}".Trim();
                var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
                var newUser = new User
                {
                    FullName = fullName,
                    Email = normalizedEmail,
                    PhoneNumber = phoneNumber,
                    PasswordHash = passwordHash,
                    RoleId = role.RoleId,
                    IsActive = true
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                var accessLevel = isSuperAdmin ? "SuperAdmin" : "Admin";
                var department = string.IsNullOrWhiteSpace(request.Department) ? "Administration" : request.Department.Trim();

                var newAdmin = new Admin
                {
                    UserId = newUser.UserId,
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    PhoneNumber = phoneNumber,
                    Department = department,
                    AccessLevel = accessLevel,
                    SecurePinHash = pinHash // ONLY hashed PIN is saved in DB
                };

                _context.Admins.Add(newAdmin);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "AdminManagement: Created new {AccessLevel} '{FullName}' ({Email}) with AdminId {AdminId}. Stored BCrypt SecurePinHash.",
                    accessLevel, fullName, newUser.Email, newAdmin.AdminId);

                return new CreateAdminResponseDto
                {
                    AdminId = newAdmin.AdminId,
                    FullName = fullName,
                    Email = newUser.Email,
                    Role = accessLevel,
                    PhoneNumber = newAdmin.PhoneNumber,
                    GeneratedPin = rawPin, // Returned ONCE for Super Admin display
                    CreatedAt = newAdmin.CreatedAt
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "AdminManagement: Error occurred while creating admin {Email}", request.Email);
                throw;
            }
        }

        /// <summary>
        /// Updates an administrator's profile, role, status, and department in Admins table.
        /// </summary>
        public async Task<UpdateAdminResponseDto> UpdateAdministratorAsync(int id, UpdateAdminDto request)
        {
            var admin = await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AdminId == id || a.UserId == id);

            if (admin == null)
            {
                throw new KeyNotFoundException($"Administrator with ID {id} was not found.");
            }

            var isSuperAdmin = request.AccessLevel.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                               request.AccessLevel.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase);
            var normalizedAccessLevel = isSuperAdmin ? "SuperAdmin" : "Admin";

            // Update user record
            var fullName = $"{request.FirstName} {request.LastName}".Trim();
            var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

            if (admin.User != null)
            {
                admin.User.FullName = fullName;
                admin.User.PhoneNumber = phoneNumber;
                admin.User.IsActive = request.IsActive;

                var targetRoleName = isSuperAdmin ? "SUPER_ADMIN" : "ADMIN";
                var role = await _context.Roles.FirstOrDefaultAsync(r =>
                    r.RoleName == targetRoleName ||
                    r.RoleName == normalizedAccessLevel);

                if (role != null)
                {
                    admin.User.RoleId = role.RoleId;
                }
                admin.User.UpdatedAt = DateTime.UtcNow;
            }

            // Update admin record
            admin.FirstName = request.FirstName.Trim();
            admin.LastName = request.LastName.Trim();
            admin.PhoneNumber = phoneNumber;
            admin.AccessLevel = normalizedAccessLevel;
            admin.Department = string.IsNullOrWhiteSpace(request.Department) ? "Administration" : request.Department.Trim();
            admin.UpdatedAt = DateTime.UtcNow;

            string? newPin = null;
            if (request.RegeneratePin)
            {
                newPin = RandomNumberGenerator.GetInt32(1000, 10000).ToString();
                admin.SecurePinHash = BCrypt.Net.BCrypt.HashPassword(newPin);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("AdminManagement: Updated admin {AdminId} ({FullName})", id, fullName);

            return new UpdateAdminResponseDto
            {
                AdminId = admin.AdminId,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                FullName = fullName,
                Email = admin.User?.Email ?? "",
                PhoneNumber = admin.PhoneNumber,
                AccessLevel = normalizedAccessLevel,
                SecurePin = "••••",
                Department = admin.Department,
                IsActive = admin.User?.IsActive ?? request.IsActive,
                NewPin = newPin,
                UpdatedAt = admin.UpdatedAt
            };
        }

        /// <summary>
        /// Regenerates an administrator's 4-digit PIN, hashes it with BCrypt, saves to DB,
        /// and returns the newly generated plaintext PIN once for display.
        /// </summary>
        public async Task<RegeneratePinResponseDto> RegeneratePinAsync(int id)
        {
            var admin = await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AdminId == id || a.UserId == id);

            if (admin == null)
            {
                throw new KeyNotFoundException($"Administrator with ID {id} was not found.");
            }

            var newPin = RandomNumberGenerator.GetInt32(1000, 10000).ToString();
            admin.SecurePinHash = BCrypt.Net.BCrypt.HashPassword(newPin);
            admin.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var fullName = !string.IsNullOrWhiteSpace(admin.FullName)
                ? admin.FullName
                : (admin.User?.FullName ?? "Admin");

            _logger.LogInformation("AdminManagement: Regenerated PIN for admin {AdminId} ({FullName}). Stored new SecurePinHash.",
                id, fullName);

            return new RegeneratePinResponseDto
            {
                AdminId = admin.AdminId,
                FullName = fullName,
                NewGeneratedPin = newPin, // Returned ONCE for display
                UpdatedAt = admin.UpdatedAt
            };
        }

        /// <summary>
        /// Permanently hard-deletes an administrator and linked User from PostgreSQL.
        /// Blocks self-deletion if id equals currentUserId.
        /// </summary>
        public async Task<bool> DeleteAdministratorAsync(int id, int currentUserId)
        {
            var admin = await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AdminId == id || a.UserId == id);

            if (admin == null)
            {
                return false;
            }

            if (admin.UserId == currentUserId || admin.AdminId == currentUserId)
            {
                throw new InvalidOperationException("You cannot delete your own active Super Admin account.");
            }

            var linkedUser = admin.User;
            _context.Admins.Remove(admin);
            if (linkedUser != null)
            {
                _context.Users.Remove(linkedUser);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("AdminManagement: Hard deleted admin {AdminId} (UserId {UserId}) by Super Admin {CallerId}",
                admin.AdminId, admin.UserId, currentUserId);

            return true;
        }

        /// <summary>
        /// Computes metrics { totalAdmins, activeAdmins, superAdmins } from Admins table.
        /// </summary>
        public async Task<AdminMetricsDto> GetAdminMetricsAsync()
        {
            var total = await _context.Admins.CountAsync();
            var active = await _context.Admins.CountAsync(a => a.User != null && a.User.IsActive);
            var supers = await _context.Admins.CountAsync(a => a.AccessLevel.ToLower() == "superadmin");

            return new AdminMetricsDto
            {
                TotalAdmins = total,
                ActiveAdmins = active,
                SuperAdmins = supers
            };
        }

        // =========================================================================
        // LEGACY & SETTINGS METHODS
        // =========================================================================

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
                FullName = a.User?.FullName ?? a.FullName,
                Email = a.User?.Email ?? "Unknown",
                AccessLevel = a.AccessLevel,
                SecurePin = "••••",
                IsActive = a.User?.IsActive ?? false,
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        public async Task<AdminResponseDto> CreateAdminAsync(CreateAdminRequestDto request)
        {
            var res = await CreateAdministratorAsync(request);
            return new AdminResponseDto
            {
                AdminId = res.AdminId,
                UserId = res.AdminId,
                FullName = res.FullName,
                Email = res.Email,
                AccessLevel = res.Role,
                SecurePin = res.GeneratedPin,
                IsActive = true,
                CreatedAt = res.CreatedAt
            };
        }

        public async Task<bool> VerifyPinAsync(int userId, string pin)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId || a.AdminId == userId);
            if (admin == null || string.IsNullOrEmpty(admin.SecurePinHash))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(pin, admin.SecurePinHash);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "VerifyPinAsync: BCrypt verification failed for UserId {UserId}", userId);
                return false;
            }
        }

        public async Task<GeneratePinResponseDto> GenerateCandidatePinAsync(int userId)
        {
            // Cryptographically generate random candidate 4-digit PIN
            var candidate = RandomNumberGenerator.GetInt32(1000, 10000).ToString();
            return await Task.FromResult(new GeneratePinResponseDto { Pin = candidate, AlreadyInUse = false });
        }

        public async Task<bool> SaveNewPinAsync(int userId, string currentPin, string newPin)
        {
            var isCorrect = await VerifyPinAsync(userId, currentPin);
            if (!isCorrect) return false;

            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId || a.AdminId == userId);
            if (admin == null) return false;

            admin.SecurePinHash = BCrypt.Net.BCrypt.HashPassword(newPin);
            admin.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash)) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateProfileAsync(int userId, string fullName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return false;

            user.FullName = fullName.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId || a.AdminId == userId);
            if (admin != null)
            {
                var parts = fullName.Trim().Split(' ', 2);
                admin.FirstName = parts[0];
                admin.LastName = parts.Length > 1 ? parts[1] : "";
                admin.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
