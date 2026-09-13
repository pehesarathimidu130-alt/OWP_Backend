using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class VendorContentService : IVendorContentService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public VendorContentService(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<List<VendorServiceResponseDto>> GetServicesAsync(int userId)
        {
            var vendorId = await GetVendorIdAsync(userId);
            return await _context.VendorServices
                .Where(service => service.VendorId == vendorId)
                .OrderBy(service => service.ServiceName)
                .Select(service => new VendorServiceResponseDto
                {
                    ServiceId = service.ServiceId,
                    ServiceName = service.ServiceName,
                    Category = service.Category,
                    Description = service.Description,
                    Price = service.Price
                })
                .ToListAsync();
        }

        public async Task<VendorServiceResponseDto> AddServiceAsync(int userId, VendorServiceRequestDto request)
        {
            var vendorId = await GetVendorIdAsync(userId);
            ValidateCategory(request.Category);
            var service = new VendorService
            {
                VendorId = vendorId,
                ServiceName = request.ServiceName.Trim(),
                Category = NormalizeCategory(request.Category),
                Description = request.Description?.Trim(),
                Price = request.Price
            };

            _context.VendorServices.Add(service);
            await _context.SaveChangesAsync();
            return MapService(service);
        }

        public async Task<VendorServiceResponseDto> UpdateServiceAsync(int userId, int serviceId, VendorServiceRequestDto request)
        {
            var vendorId = await GetVendorIdAsync(userId);
            ValidateCategory(request.Category);
            var service = await _context.VendorServices.FirstOrDefaultAsync(item => item.ServiceId == serviceId && item.VendorId == vendorId)
                ?? throw new KeyNotFoundException("Service was not found.");
            service.ServiceName = request.ServiceName.Trim();
            service.Category = NormalizeCategory(request.Category);
            service.Description = request.Description?.Trim();
            service.Price = request.Price;
            await _context.SaveChangesAsync();
            return MapService(service);
        }

        public async Task DeleteServiceAsync(int userId, int serviceId)
        {
            var vendorId = await GetVendorIdAsync(userId);
            var service = await _context.VendorServices.FirstOrDefaultAsync(item => item.ServiceId == serviceId && item.VendorId == vendorId)
                ?? throw new KeyNotFoundException("Service was not found.");
            _context.VendorServices.Remove(service);
            await _context.SaveChangesAsync();
        }

        public async Task<List<VendorPerformanceResponseDto>> GetPerformancesAsync(int userId)
        {
            var vendorId = await GetVendorIdAsync(userId);
            return await _context.VendorPerformances
                .Where(performance => performance.VendorId == vendorId)
                .OrderByDescending(performance => performance.EventDate ?? performance.CreatedAt)
                .Select(performance => new VendorPerformanceResponseDto
                {
                    PerformanceId = performance.PerformanceId,
                    Title = performance.Title,
                    Category = performance.Category,
                    Description = performance.Description,
                    PhotoUrl = performance.PhotoUrl,
                    CustomerName = performance.CustomerName,
                    CustomerFeedback = performance.CustomerFeedback,
                    EventDate = performance.EventDate
                })
                .ToListAsync();
        }

        public async Task<VendorPerformanceResponseDto> AddPerformanceAsync(int userId, VendorPerformanceRequestDto request, IFormFile? photo)
        {
            var vendorId = await GetVendorIdAsync(userId);
            ValidateCategory(request.Category);
            var performance = new VendorPerformance
            {
                VendorId = vendorId,
                Title = request.Title.Trim(),
                Category = NormalizeCategory(request.Category),
                Description = request.Description?.Trim(),
                PhotoUrl = await SavePhotoAsync(photo),
                CustomerName = request.CustomerName?.Trim(),
                CustomerFeedback = request.CustomerFeedback?.Trim(),
                EventDate = request.EventDate
            };

            _context.VendorPerformances.Add(performance);
            await _context.SaveChangesAsync();
            return MapPerformance(performance);
        }

        public async Task<VendorPerformanceResponseDto> UpdatePerformanceAsync(int userId, int performanceId, VendorPerformanceRequestDto request, IFormFile? photo)
        {
            var vendorId = await GetVendorIdAsync(userId);
            ValidateCategory(request.Category);
            var performance = await _context.VendorPerformances.FirstOrDefaultAsync(item => item.PerformanceId == performanceId && item.VendorId == vendorId)
                ?? throw new KeyNotFoundException("Performance was not found.");
            performance.Title = request.Title.Trim();
            performance.Category = NormalizeCategory(request.Category);
            performance.Description = request.Description?.Trim();
            performance.CustomerName = request.CustomerName?.Trim();
            performance.CustomerFeedback = request.CustomerFeedback?.Trim();
            performance.EventDate = request.EventDate;
            if (photo != null)
            {
                DeletePhoto(performance.PhotoUrl);
                performance.PhotoUrl = await SavePhotoAsync(photo);
            }
            await _context.SaveChangesAsync();
            return MapPerformance(performance);
        }

        public async Task DeletePerformanceAsync(int userId, int performanceId)
        {
            var vendorId = await GetVendorIdAsync(userId);
            var performance = await _context.VendorPerformances.FirstOrDefaultAsync(item => item.PerformanceId == performanceId && item.VendorId == vendorId)
                ?? throw new KeyNotFoundException("Performance was not found.");
            DeletePhoto(performance.PhotoUrl);
            _context.VendorPerformances.Remove(performance);
            await _context.SaveChangesAsync();
        }

        public async Task<List<NotificationResponseDto>> GetNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Where(notification => notification.UserId == userId)
                .OrderByDescending(notification => notification.CreatedAt)
                .Select(notification => new NotificationResponseDto
                {
                    NotificationId = notification.NotificationId,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt
                })
                .ToListAsync();
        }

        private async Task<int> GetVendorIdAsync(int userId)
        {
            var vendorId = await _context.Vendors
                .Where(vendor => vendor.UserId == userId)
                .Select(vendor => (int?)vendor.VendorId)
                .FirstOrDefaultAsync();

            return vendorId ?? throw new KeyNotFoundException("Vendor profile was not found for this user.");
        }

        private static void ValidateCategory(string category)
        {
            if (!VendorService.AllowedCategories.Contains(category.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Category must be Photography, Decorations, Catering, or Music.");
            }
        }

        private static string NormalizeCategory(string category)
        {
            return VendorService.AllowedCategories.First(allowed => string.Equals(allowed, category.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static VendorServiceResponseDto MapService(VendorService service) => new()
        {
            ServiceId = service.ServiceId,
            ServiceName = service.ServiceName,
            Category = service.Category,
            Description = service.Description,
            Price = service.Price
        };

        private static VendorPerformanceResponseDto MapPerformance(VendorPerformance performance) => new()
        {
            PerformanceId = performance.PerformanceId,
            Title = performance.Title,
            Category = performance.Category,
            Description = performance.Description,
            PhotoUrl = performance.PhotoUrl,
            CustomerName = performance.CustomerName,
            CustomerFeedback = performance.CustomerFeedback,
            EventDate = performance.EventDate
        };

        private async Task<string?> SavePhotoAsync(IFormFile? photo)
        {
            if (photo == null || photo.Length == 0)
            {
                return null;
            }
            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Only JPG, JPEG, PNG, and WEBP images are supported.");
            }
            if (photo.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException("Photos must be 5 MB or smaller.");
            }
            var folder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "vendor-performance");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            await using var stream = File.Create(Path.Combine(folder, fileName));
            await photo.CopyToAsync(stream);
            return $"/uploads/vendor-performance/{fileName}";
        }

        private void DeletePhoto(string? photoUrl)
        {
            if (string.IsNullOrWhiteSpace(photoUrl)) return;
            var filePath = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), photoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }
}
