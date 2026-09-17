using Backend.DTOs;

namespace Backend.Services
{
    public interface IVendorContentService
    {
        Task<List<VendorServiceResponseDto>> GetServicesAsync(int userId);
        Task<VendorServiceResponseDto> GetServiceByIdAsync(int userId, int serviceId);
        Task<VendorServiceResponseDto> AddServiceAsync(int userId, VendorServiceRequestDto request);
        Task<VendorServiceResponseDto> UpdateServiceAsync(int userId, int serviceId, VendorServiceRequestDto request);
        Task DeleteServiceAsync(int userId, int serviceId);
        Task<VendorServiceImageDto> UploadServiceImageAsync(int userId, int serviceId, IFormFile file, bool isCover = false);
        Task DeleteServiceImageAsync(int userId, int serviceId, int imageId);

        Task<List<VendorPerformanceResponseDto>> GetPerformancesAsync(int userId);
        Task<VendorPerformanceResponseDto> AddPerformanceAsync(int userId, VendorPerformanceRequestDto request, IFormFile? photo);
        Task<VendorPerformanceResponseDto> UpdatePerformanceAsync(int userId, int performanceId, VendorPerformanceRequestDto request, IFormFile? photo);
        Task DeletePerformanceAsync(int userId, int performanceId);

        Task<List<NotificationResponseDto>> GetNotificationsAsync(int userId);
    }
}
