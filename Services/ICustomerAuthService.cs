using Backend.DTOs;

namespace Backend.Services
{
    public interface ICustomerAuthService
    {
        Task<CustomerAuthResponseDto> RegisterAsync(CustomerRegisterRequestDto request);
        Task<CustomerAuthResponseDto> LoginAsync(CustomerLoginRequestDto request);
    }
}
