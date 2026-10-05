using Backend.DTOs;

namespace Backend.Services
{
    public interface IVendorRegistrationService
    {
        Task<RegistrationOptionsResponse> GetOptionsAsync();
        Task<LoginResponseDto> RegisterAsync(VendorRegistrationRequest request);
    }
}
