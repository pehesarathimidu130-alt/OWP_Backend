using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Backend.Services
{
    public class VendorProfileService : IVendorProfileService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public VendorProfileService(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<VendorProfileResponseDto> GetProfileAsync(int userId)
        {
            var vendor = await GetOrCreateVendorAsync(userId);
            return MapToResponse(vendor);
        }

        public async Task<VendorProfileResponseDto> UpdateProfileAsync(int userId, VendorProfileUpdateDto request)
        {
            var vendor = await GetOrCreateVendorAsync(userId);

            vendor.BusinessName = request.BusinessName.Trim();
            vendor.Category = request.Category?.Trim();
            vendor.Tagline = request.Tagline?.Trim();
            vendor.Description = request.Description?.Trim();
            vendor.OwnerName = request.OwnerName?.Trim();
            vendor.ContactNumber = request.ContactNumber?.Trim();
            vendor.AltPhoneNumber = request.AltPhoneNumber?.Trim();
            vendor.Email = request.Email?.Trim();
            vendor.WebsiteUrl = request.WebsiteUrl?.Trim();
            vendor.Address = request.Address?.Trim();
            vendor.City = request.City?.Trim();
            vendor.State = request.State?.Trim();
            vendor.PostalCode = request.PostalCode?.Trim();
            vendor.Country = request.Country?.Trim();
            vendor.ServiceAreas = request.ServiceAreas?.Trim();
            vendor.TravelPolicy = request.TravelPolicy?.Trim();
            vendor.YearsInBusiness = request.YearsInBusiness;

            if (request.BusinessHours != null)
            {
                vendor.BusinessHoursJson = JsonSerializer.Serialize(request.BusinessHours, _jsonOptions);
            }

            if (request.SocialLinks != null)
            {
                vendor.SocialLinksJson = JsonSerializer.Serialize(request.SocialLinks, _jsonOptions);
            }

            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = "Business Profile Updated",
                Message = $"Your business profile for \"{vendor.BusinessName}\" was updated successfully.",
                Type = "Info",
                IsRead = false
            });

            await _context.SaveChangesAsync();
            return MapToResponse(vendor);
        }

        public async Task<string?> UploadLogoAsync(int userId, IFormFile file)
        {
            var vendor = await GetOrCreateVendorAsync(userId);
            var logoUrl = await SaveFileAsync(file, "logos", new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" }, 5 * 1024 * 1024);

            if (!string.IsNullOrEmpty(vendor.LogoUrl))
            {
                DeleteFile(vendor.LogoUrl);
            }

            vendor.LogoUrl = logoUrl;
            await _context.SaveChangesAsync();
            return logoUrl;
        }

        public async Task RemoveLogoAsync(int userId)
        {
            var vendor = await GetOrCreateVendorAsync(userId);
            if (!string.IsNullOrEmpty(vendor.LogoUrl))
            {
                DeleteFile(vendor.LogoUrl);
                vendor.LogoUrl = null;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string?> UploadCoverImageAsync(int userId, IFormFile file)
        {
            var vendor = await GetOrCreateVendorAsync(userId);
            var coverUrl = await SaveFileAsync(file, "covers", new[] { ".jpg", ".jpeg", ".png", ".webp" }, 10 * 1024 * 1024);

            if (!string.IsNullOrEmpty(vendor.CoverImageUrl))
            {
                DeleteFile(vendor.CoverImageUrl);
            }

            vendor.CoverImageUrl = coverUrl;
            await _context.SaveChangesAsync();
            return coverUrl;
        }

        public async Task RemoveCoverImageAsync(int userId)
        {
            var vendor = await GetOrCreateVendorAsync(userId);
            if (!string.IsNullOrEmpty(vendor.CoverImageUrl))
            {
                DeleteFile(vendor.CoverImageUrl);
                vendor.CoverImageUrl = null;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<VendorGalleryImageDto> AddGalleryImageAsync(int userId, IFormFile file, string? caption, string? category)
        {
            var vendor = await GetOrCreateVendorAsync(userId);
            var imageUrl = await SaveFileAsync(file, "gallery", new[] { ".jpg", ".jpeg", ".png", ".webp" }, 10 * 1024 * 1024);

            var nextOrder = await _context.VendorGalleryImages
                .Where(g => g.VendorId == vendor.VendorId)
                .Select(g => (int?)g.DisplayOrder)
                .MaxAsync() ?? 0;

            var galleryImage = new VendorGalleryImage
            {
                VendorId = vendor.VendorId,
                ImageUrl = imageUrl,
                Caption = caption?.Trim(),
                Category = category?.Trim(),
                DisplayOrder = nextOrder + 1,
                IsFeatured = false
            };

            _context.VendorGalleryImages.Add(galleryImage);
            await _context.SaveChangesAsync();

            return new VendorGalleryImageDto
            {
                ImageId = galleryImage.ImageId,
                ImageUrl = galleryImage.ImageUrl,
                Caption = galleryImage.Caption,
                Category = galleryImage.Category,
                DisplayOrder = galleryImage.DisplayOrder,
                IsFeatured = galleryImage.IsFeatured
            };
        }

        public async Task<VendorGalleryImageDto> UpdateGalleryImageAsync(int userId, int imageId, VendorGalleryUpdateRequestDto request)
        {
            var vendor = await GetOrCreateVendorAsync(userId);
            var image = await _context.VendorGalleryImages
                .FirstOrDefaultAsync(g => g.ImageId == imageId && g.VendorId == vendor.VendorId)
                ?? throw new KeyNotFoundException("Gallery image not found.");

            if (request.Caption != null) image.Caption = request.Caption.Trim();
            if (request.Category != null) image.Category = request.Category.Trim();
            if (request.DisplayOrder.HasValue) image.DisplayOrder = request.DisplayOrder.Value;
            if (request.IsFeatured.HasValue) image.IsFeatured = request.IsFeatured.Value;

            await _context.SaveChangesAsync();

            return new VendorGalleryImageDto
            {
                ImageId = image.ImageId,
                ImageUrl = image.ImageUrl,
                Caption = image.Caption,
                Category = image.Category,
                DisplayOrder = image.DisplayOrder,
                IsFeatured = image.IsFeatured
            };
        }

        public async Task DeleteGalleryImageAsync(int userId, int imageId)
        {
            var vendor = await GetOrCreateVendorAsync(userId);
            var image = await _context.VendorGalleryImages
                .FirstOrDefaultAsync(g => g.ImageId == imageId && g.VendorId == vendor.VendorId)
                ?? throw new KeyNotFoundException("Gallery image not found.");

            DeleteFile(image.ImageUrl);
            _context.VendorGalleryImages.Remove(image);
            await _context.SaveChangesAsync();
        }

        public async Task<VendorDocumentDto> UploadDocumentAsync(int userId, IFormFile file, string documentName, string documentType)
        {
            var vendor = await GetOrCreateVendorAsync(userId);
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
            var fileUrl = await SaveFileAsync(file, "documents", allowedExtensions, 15 * 1024 * 1024);

            var doc = new VendorDocument
            {
                VendorId = vendor.VendorId,
                DocumentName = string.IsNullOrWhiteSpace(documentName) ? Path.GetFileNameWithoutExtension(file.FileName) : documentName.Trim(),
                DocumentType = string.IsNullOrWhiteSpace(documentType) ? "Business Registration" : documentType.Trim(),
                FileUrl = fileUrl,
                Status = "Pending"
            };

            _context.VendorDocuments.Add(doc);
            await _context.SaveChangesAsync();

            return new VendorDocumentDto
            {
                DocumentId = doc.DocumentId,
                DocumentName = doc.DocumentName,
                DocumentType = doc.DocumentType,
                FileUrl = doc.FileUrl,
                Status = doc.Status,
                UploadedAt = doc.CreatedAt
            };
        }

        public async Task DeleteDocumentAsync(int userId, int documentId)
        {
            var vendor = await GetOrCreateVendorAsync(userId);
            var doc = await _context.VendorDocuments
                .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.VendorId == vendor.VendorId)
                ?? throw new KeyNotFoundException("Document not found.");

            DeleteFile(doc.FileUrl);
            _context.VendorDocuments.Remove(doc);
            await _context.SaveChangesAsync();
        }

        private async Task<Vendor> GetOrCreateVendorAsync(int userId)
        {
            var vendor = await _context.Vendors
                .Include(v => v.GalleryImages)
                .Include(v => v.Documents)
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (vendor == null)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                vendor = new Vendor
                {
                    UserId = userId,
                    BusinessName = user?.FullName ?? "My Wedding Business",
                    ContactNumber = user?.PhoneNumber,
                    Email = user?.Email,
                    Status = "Approved",
                    IsApproved = true,
                    VerificationStatus = "Pending"
                };

                _context.Vendors.Add(vendor);
                await _context.SaveChangesAsync();
            }

            return vendor;
        }

        private static VendorProfileResponseDto MapToResponse(Vendor vendor)
        {
            List<BusinessHoursItemDto> hours = new();
            if (!string.IsNullOrWhiteSpace(vendor.BusinessHoursJson))
            {
                try
                {
                    hours = JsonSerializer.Deserialize<List<BusinessHoursItemDto>>(vendor.BusinessHoursJson, _jsonOptions) ?? new();
                }
                catch { }
            }

            SocialLinksDto socials = new();
            if (!string.IsNullOrWhiteSpace(vendor.SocialLinksJson))
            {
                try
                {
                    socials = JsonSerializer.Deserialize<SocialLinksDto>(vendor.SocialLinksJson, _jsonOptions) ?? new();
                }
                catch { }
            }

            return new VendorProfileResponseDto
            {
                VendorId = vendor.VendorId,
                UserId = vendor.UserId,
                BusinessName = vendor.BusinessName,
                Category = vendor.Category,
                Tagline = vendor.Tagline,
                Description = vendor.Description,
                OwnerName = vendor.OwnerName,
                ContactNumber = vendor.ContactNumber,
                AltPhoneNumber = vendor.AltPhoneNumber,
                Email = vendor.Email,
                WebsiteUrl = vendor.WebsiteUrl,
                Address = vendor.Address,
                City = vendor.City,
                State = vendor.State,
                PostalCode = vendor.PostalCode,
                Country = vendor.Country,
                ServiceAreas = vendor.ServiceAreas,
                TravelPolicy = vendor.TravelPolicy,
                YearsInBusiness = vendor.YearsInBusiness,
                IsApproved = vendor.IsApproved,
                Status = vendor.Status,
                VerificationStatus = vendor.VerificationStatus ?? "Pending",
                LogoUrl = vendor.LogoUrl,
                CoverImageUrl = vendor.CoverImageUrl,
                BusinessHours = hours,
                SocialLinks = socials,
                GalleryImages = vendor.GalleryImages
                    .OrderBy(g => g.DisplayOrder)
                    .Select(g => new VendorGalleryImageDto
                    {
                        ImageId = g.ImageId,
                        ImageUrl = g.ImageUrl,
                        Caption = g.Caption,
                        Category = g.Category,
                        DisplayOrder = g.DisplayOrder,
                        IsFeatured = g.IsFeatured
                    }).ToList(),
                Documents = vendor.Documents
                    .OrderByDescending(d => d.CreatedAt)
                    .Select(d => new VendorDocumentDto
                    {
                        DocumentId = d.DocumentId,
                        DocumentName = d.DocumentName,
                        DocumentType = d.DocumentType,
                        FileUrl = d.FileUrl,
                        Status = d.Status,
                        UploadedAt = d.CreatedAt
                    }).ToList()
            };
        }

        private async Task<string> SaveFileAsync(IFormFile? file, string subfolder, string[] allowedExtensions, long maxSizeBytes)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Please provide a valid file.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"Unsupported file format '{extension}'. Allowed: {string.Join(", ", allowedExtensions)}");
            }

            if (file.Length > maxSizeBytes)
            {
                throw new ArgumentException($"File size exceeds limit of {maxSizeBytes / (1024 * 1024)} MB.");
            }

            var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(rootPath, "uploads", "vendor-profile", subfolder);
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var destinationPath = Path.Combine(folder, fileName);

            await using var stream = File.Create(destinationPath);
            await file.CopyToAsync(stream);

            return $"/uploads/vendor-profile/{subfolder}/{fileName}";
        }

        private void DeleteFile(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl)) return;
            try
            {
                var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var cleanPath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(rootPath, cleanPath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch { }
        }
    }
}
