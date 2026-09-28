using Backend.DTOs;

namespace Backend.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Authenticates a user (Vendor or Admin) and returns a JWT token.
        /// For Admin login, also validates the 4-digit SecurePin against the Admins table.
        /// </summary>
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

        /// <summary>
        /// Builds a LoginResponseDto for a newly registered Vendor (generates JWT, returns token + profile).
        /// The User must already have its Role navigation loaded or the RoleId set to the Vendor role.
        /// </summary>
        LoginResponseDto BuildVendorLoginResponse(Entities.User user);
    }
}
