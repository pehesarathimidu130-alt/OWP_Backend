using Backend.DTOs;

namespace Backend.Services
{
    public interface IAdminManagementService
    {
        // ── Super Admin Administrator CRUD ──

        /// <summary>
        /// Queries administrators with optional search (name/email), role, and status filters.
        /// </summary>
        Task<List<AdminListItemDto>> GetAdministratorsAsync(string? search = null, string? role = null, string? status = null);

        /// <summary>
        /// Retrieves a single administrator by AdminId/UserId.
        /// </summary>
        Task<AdminListItemDto?> GetAdministratorByIdAsync(int id);

        /// <summary>
        /// Creates a new administrator with auto-generated secure PIN, BCrypt hashing, and EF Core transaction.
        /// </summary>
        Task<CreateAdminResponseDto> CreateAdministratorAsync(CreateAdminRequestDto request);

        /// <summary>
        /// Updates an administrator's profile, access level, status, and department.
        /// </summary>
        Task<UpdateAdminResponseDto> UpdateAdministratorAsync(int id, UpdateAdminDto request);

        /// <summary>
        /// Regenerates an administrator's PIN, updates SecurePinHash in the database,
        /// and returns the newly generated PIN once.
        /// </summary>
        Task<RegeneratePinResponseDto> RegeneratePinAsync(int id);

        /// <summary>
        /// Deactivates/deletes an administrator. Blocks self-deletion if id equals currentUserId.
        /// </summary>
        Task<bool> DeleteAdministratorAsync(int id, int currentUserId);

        /// <summary>
        /// Computes metrics { totalAdmins, activeAdmins, superAdmins } for the dashboard stat counters.
        /// </summary>
        Task<AdminMetricsDto> GetAdminMetricsAsync();

        // ── Legacy & Admin Settings Methods ──

        Task<List<AdminResponseDto>> GetAllAdminsAsync();
        Task<AdminResponseDto> CreateAdminAsync(CreateAdminRequestDto request);
        Task<bool> VerifyPinAsync(int userId, string pin);
        Task<GeneratePinResponseDto> GenerateCandidatePinAsync(int userId);
        Task<bool> SaveNewPinAsync(int userId, string currentPin, string newPin);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> UpdateProfileAsync(int userId, string fullName);
    }
}
