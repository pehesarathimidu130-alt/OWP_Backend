using Backend.DTOs;

namespace Backend.Services
{
    public interface IVendorContentService
    {
        Task<List<VendorServiceResponseDto>> GetServicesAsync(int userId);
        Task<VendorServiceResponseDto> AddServiceAsync(int userId, VendorServiceRequestDto request);
        Task<VendorServiceResponseDto> UpdateServiceAsync(int userId, int serviceId, VendorServiceRequestDto request);
        Task DeleteServiceAsync(int userId, int serviceId);
        Task<List<VendorPerformanceResponseDto>> GetPerformancesAsync(int userId);
        Task<VendorPerformanceResponseDto> AddPerformanceAsync(int userId, VendorPerformanceRequestDto request, IFormFile? photo);
        Task<VendorPerformanceResponseDto> UpdatePerformanceAsync(int userId, int performanceId, VendorPerformanceRequestDto request, IFormFile? photo);
        Task DeletePerformanceAsync(int userId, int performanceId);
        Task<List<NotificationResponseDto>> GetNotificationsAsync(int userId);
    }
}
