using Backend.DTOs;

namespace Backend.Services
{
    public interface ICustomerAuthService
    {
        Task<CustomerAuthResponseDto> RegisterAsync(CustomerRegisterRequestDto request);
        Task<CustomerAuthResponseDto> LoginAsync(CustomerLoginRequestDto request);
        Task<bool> ForgotPasswordAsync(CustomerForgotPasswordRequestDto request);
        Task<bool> ResetPasswordAsync(CustomerResetPasswordRequestDto request);
    }
}
