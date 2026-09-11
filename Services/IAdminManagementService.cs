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

        /// <summary>
        /// Verifies that the supplied PIN matches the stored PIN for the given admin (by UserId).
        /// Returns true if correct, false if wrong.
        /// </summary>
        Task<bool> VerifyPinAsync(int userId, string pin);

        /// <summary>
        /// Generates a unique random 4-digit PIN candidate for the given admin.
        /// Does NOT write to the database. Call SaveNewPinAsync to commit.
        /// </summary>
        Task<GeneratePinResponseDto> GenerateCandidatePinAsync(int userId);

        /// <summary>
        /// Re-verifies the admin's current PIN and, if correct, saves the supplied new PIN.
        /// Returns false if the current PIN is wrong.
        /// </summary>
        Task<bool> SaveNewPinAsync(int userId, string currentPin, string newPin);

        /// <summary>
        /// Verifies current password and updates the admin's password with BCrypt hashing.
        /// Returns true if successful, false if current password is incorrect.
        /// </summary>
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        /// <summary>
        /// Updates the admin user's display full name.
        /// </summary>
        Task<bool> UpdateProfileAsync(int userId, string fullName);
    }
}

