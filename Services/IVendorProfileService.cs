using Backend.DTOs;
using Microsoft.AspNetCore.Http;

namespace Backend.Services
{
    public interface IVendorProfileService
    {
        Task<VendorProfileResponseDto> GetProfileAsync(int userId);
        Task<VendorProfileResponseDto> UpdateProfileAsync(int userId, VendorProfileUpdateDto request);
        Task<string?> UploadLogoAsync(int userId, IFormFile file);
        Task RemoveLogoAsync(int userId);
        Task<string?> UploadCoverImageAsync(int userId, IFormFile file);
        Task RemoveCoverImageAsync(int userId);
        Task<VendorGalleryImageDto> AddGalleryImageAsync(int userId, IFormFile file, string? caption, string? category);
        Task<VendorGalleryImageDto> UpdateGalleryImageAsync(int userId, int imageId, VendorGalleryUpdateRequestDto request);
        Task DeleteGalleryImageAsync(int userId, int imageId);
        Task<VendorDocumentDto> UploadDocumentAsync(int userId, IFormFile file, string documentName, string documentType);
        Task DeleteDocumentAsync(int userId, int documentId);
    }
}
