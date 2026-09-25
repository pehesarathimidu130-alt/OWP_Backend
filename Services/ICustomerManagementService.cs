using Backend.DTOs;

namespace Backend.Services
{
    public interface ICustomerManagementService
    {
        Task<List<CustomerManagementResponseDto>> GetAllCustomersAsync();
        Task<bool> ToggleCustomerStatusAsync(int id, string newStatus);
        Task<bool> DeleteCustomerAsync(int id);
    }
}
