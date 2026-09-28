using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    public class VendorRatingsController : ControllerBase
    {
        private readonly IVendorRatingService _ratingService;
        private readonly AppDbContext _context;
        private readonly ILogger<VendorRatingsController> _logger;

        public VendorRatingsController(
            IVendorRatingService ratingService,
            AppDbContext context,
            ILogger<VendorRatingsController> logger)
        {
            _ratingService = ratingService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/vendors/{vendorId}/ratings
        /// Submits or updates a rating (1-5) for a vendor, scoped strictly to the authenticated customer.
        /// </summary>
        [HttpPost("api/vendors/{vendorId:int}/ratings")]
        [HttpPut("api/vendors/{vendorId:int}/ratings")]
        [HttpPost("api/ratings")]
        [Authorize]
        [ProducesResponseType(typeof(VendorRatingResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SubmitOrUpdateRating([FromRoute] int? vendorId, [FromBody] SubmitVendorRatingDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Rating data is required." });
            }

            var targetVendorId = vendorId ?? dto.VendorId;
            if (!targetVendorId.HasValue || targetVendorId.Value <= 0)
            {
                return BadRequest(new { message = "A valid vendor ID must be provided." });
            }

            if (dto.RatingValue < 1 || dto.RatingValue > 5)
            {
                return BadRequest(new { message = "Rating value must be an integer between 1 and 5." });
            }

            try
            {
                var customer = await ResolveAuthenticatedCustomerAsync();
                var result = await _ratingService.SubmitOrUpdateRatingAsync(
                    customer.CustomerId,
                    targetVendorId.Value,
                    dto.RatingValue
                );

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting rating for vendor {VendorId}", targetVendorId);
                return Problem(
                    detail: "An unexpected error occurred while processing the rating.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }

        /// <summary>
        /// GET /api/vendors/{vendorId}/ratings/summary
        /// Returns aggregate average rating and total rating count computed at query time.
        /// </summary>
        [HttpGet("api/vendors/{vendorId:int}/ratings/summary")]
        [HttpGet("api/vendors/{vendorId:int}/ratings")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(VendorRatingSummaryDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRatingSummary(int vendorId)
        {
            var summary = await _ratingService.GetVendorRatingSummaryAsync(vendorId);
            return Ok(summary);
        }

        /// <summary>
        /// GET /api/vendors/{vendorId}/ratings/my-rating
        /// Returns the authenticated customer's current rating for this vendor, if one exists.
        /// </summary>
        [HttpGet("api/vendors/{vendorId:int}/ratings/my-rating")]
        [Authorize]
        [ProducesResponseType(typeof(CustomerVendorRatingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyRating(int vendorId)
        {
            try
            {
                var customer = await ResolveAuthenticatedCustomerAsync();
                var rating = await _ratingService.GetCustomerRatingForVendorAsync(customer.CustomerId, vendorId);

                if (rating == null)
                {
                    return NotFound(new { message = "No rating submitted by this customer for the vendor.", rated = false });
                }

                return Ok(rating);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer rating for vendor {VendorId}", vendorId);
                return Problem(
                    detail: "An unexpected error occurred while retrieving your rating.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }

        private async Task<Customer> ResolveAuthenticatedCustomerAsync()
        {
            // 1. Direct customerId claim if present
            var customerIdClaim = User.FindFirst("customerId")?.Value
                ?? User.FindFirst("CustomerId")?.Value;
            if (int.TryParse(customerIdClaim, out var cId))
            {
                var cust = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == cId);
                if (cust != null) return cust;
            }

            // 2. UserId lookup
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("userId")?.Value
                ?? User.FindFirst("id")?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                var cust = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (cust != null) return cust;
            }

            throw new UnauthorizedAccessException("Only authenticated customers can submit or view ratings.");
        }
    }
}
