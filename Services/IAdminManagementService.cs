using Backend.DTOs;

namespace Backend.Services
{
    public interface IAdminManagementService
    {
        /// <summary>
        /// Returns all admin users with their profile details.
        /// </summary>
        Task<List<AdminResponseDto>> GetAllAdminsAsync();

        /// <summary>
        /// Creates a new Admin user with a hashed password and auto-generated 4-digit PIN.
        /// Returns the created admin details including the generated PIN.
        /// </summary>
        Task<AdminResponseDto> CreateAdminAsync(CreateAdminRequestDto request);
    }
}
