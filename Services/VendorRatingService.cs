using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class VendorRatingService : IVendorRatingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VendorRatingService> _logger;

        public VendorRatingService(AppDbContext context, ILogger<VendorRatingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<VendorRatingResponseDto> SubmitOrUpdateRatingAsync(int customerId, int vendorId, int ratingValue)
        {
            if (ratingValue < 1 || ratingValue > 5)
            {
                throw new ArgumentException("Rating value must be an integer between 1 and 5.");
            }

            // Verify vendor exists
            var vendorExists = await _context.Vendors.AnyAsync(v => v.VendorId == vendorId);
            if (!vendorExists)
            {
                throw new KeyNotFoundException($"Vendor with ID {vendorId} not found.");
            }

            // Verify customer exists
            var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == customerId);
            if (!customerExists)
            {
                throw new KeyNotFoundException($"Customer with ID {customerId} not found.");
            }

            // Check for existing rating to update in-place
            var existing = await _context.VendorRatings
                .FirstOrDefaultAsync(r => r.VendorId == vendorId && r.CustomerId == customerId);

            bool isUpdated = false;
            VendorRating rating;

            if (existing != null)
            {
                existing.RatingValue = ratingValue;
                existing.UpdatedAt = DateTime.UtcNow;
                _context.VendorRatings.Update(existing);
                rating = existing;
                isUpdated = true;
                _logger.LogInformation("Updated rating for Vendor {VendorId} by Customer {CustomerId} to {RatingValue}", vendorId, customerId, ratingValue);
            }
            else
            {
                rating = new VendorRating
                {
                    VendorId = vendorId,
                    CustomerId = customerId,
                    RatingValue = ratingValue,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.VendorRatings.AddAsync(rating);
                _logger.LogInformation("Created new rating for Vendor {VendorId} by Customer {CustomerId} with value {RatingValue}", vendorId, customerId, ratingValue);
            }

            await _context.SaveChangesAsync();

            // Compute live summary at query time
            var summary = await GetVendorRatingSummaryAsync(vendorId);

            return new VendorRatingResponseDto
            {
                Id = rating.Id,
                VendorId = vendorId,
                CustomerId = customerId,
                RatingValue = rating.RatingValue,
                CreatedAt = rating.CreatedAt,
                UpdatedAt = rating.UpdatedAt,
                IsUpdated = isUpdated,
                AverageRating = summary.AverageRating,
                TotalRatings = summary.TotalRatings,
                Message = isUpdated ? "Rating updated successfully." : "Rating submitted successfully."
            };
        }

        public async Task<VendorRatingSummaryDto> GetVendorRatingSummaryAsync(int vendorId)
        {
            var summary = await _context.VendorRatings
                .Where(r => r.VendorId == vendorId)
                .GroupBy(r => r.VendorId)
                .Select(g => new VendorRatingSummaryDto
                {
                    VendorId = g.Key,
                    AverageRating = Math.Round(g.Average(r => (double)r.RatingValue), 1),
                    TotalRatings = g.Count()
                })
                .FirstOrDefaultAsync();

            return summary ?? new VendorRatingSummaryDto
            {
                VendorId = vendorId,
                AverageRating = 0.0,
                TotalRatings = 0
            };
        }

        public async Task<Dictionary<int, VendorRatingSummaryDto>> GetVendorsRatingSummariesAsync(IEnumerable<int> vendorIds)
        {
            var distinctIds = vendorIds.Distinct().ToList();
            if (!distinctIds.Any())
            {
                return new Dictionary<int, VendorRatingSummaryDto>();
            }

            var summaries = await _context.VendorRatings
                .Where(r => distinctIds.Contains(r.VendorId))
                .GroupBy(r => r.VendorId)
                .Select(g => new VendorRatingSummaryDto
                {
                    VendorId = g.Key,
                    AverageRating = Math.Round(g.Average(r => (double)r.RatingValue), 1),
                    TotalRatings = g.Count()
                })
                .ToDictionaryAsync(s => s.VendorId);

            return summaries;
        }

        public async Task<CustomerVendorRatingDto?> GetCustomerRatingForVendorAsync(int customerId, int vendorId)
        {
            var r = await _context.VendorRatings
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.VendorId == vendorId && r.CustomerId == customerId);

            if (r == null) return null;

            return new CustomerVendorRatingDto
            {
                Id = r.Id,
                VendorId = r.VendorId,
                CustomerId = r.CustomerId,
                RatingValue = r.RatingValue,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            };
        }
    }
}
