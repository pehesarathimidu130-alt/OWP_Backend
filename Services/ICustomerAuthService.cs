using Backend.DTOs;

namespace Backend.Services
{
    public interface ICustomerAuthService
    {
        Task<CustomerAuthResponseDto> RegisterAsync(CustomerRegisterRequestDto request);
        Task<CustomerAuthResponseDto> LoginAsync(CustomerLoginRequestDto request);
        Task<CustomerAuthResponseDto> GoogleLoginAsync(GoogleSignInRequest request);
        Task<string?> ForgotPasswordAsync(CustomerForgotPasswordRequestDto request);
        Task<bool> ResetPasswordAsync(CustomerResetPasswordRequestDto request);
    }
}
