using Backend.DTOs;

namespace Backend.Services
{
    public interface IVendorRatingService
    {
        Task<VendorRatingResponseDto> SubmitOrUpdateRatingAsync(int customerId, int vendorId, int ratingValue);
        Task<VendorRatingSummaryDto> GetVendorRatingSummaryAsync(int vendorId);
        Task<Dictionary<int, VendorRatingSummaryDto>> GetVendorsRatingSummariesAsync(IEnumerable<int> vendorIds);
        Task<CustomerVendorRatingDto?> GetCustomerRatingForVendorAsync(int customerId, int vendorId);
    }
}
