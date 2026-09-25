using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/vendor-performance")]
    [Authorize]
    public class VendorPerformanceController : ControllerBase
    {
        private readonly IVendorPerformanceService _service;

        public VendorPerformanceController(IVendorPerformanceService service)
        {
            _service = service;
        }

        /// <summary>
        /// GET api/vendor-performance/active-listings
        /// Returns counts of this vendor's listings grouped by status.
        /// </summary>
        [HttpGet("active-listings")]
        [ProducesResponseType(typeof(ActiveListingsSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetActiveListings()
        {
            try
            {
                var vendorId = await ResolveVendorIdAsync();
                var result = await _service.GetActiveListingsSummaryAsync(vendorId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET api/vendor-performance/traffic?from={date}&to={date}
        /// Returns view/favorite traffic summary and top 5 listings within date range.
        /// </summary>
        [HttpGet("traffic")]
        [ProducesResponseType(typeof(TrafficSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTraffic([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            if (!from.HasValue || !to.HasValue)
            {
                return BadRequest(new { message = "Both 'from' and 'to' date parameters are required." });
            }

            if (from.Value > to.Value)
            {
                return BadRequest(new { message = "'from' date must be earlier than or equal to 'to' date." });
            }

            try
            {
                var vendorId = await ResolveVendorIdAsync();
                var result = await _service.GetTrafficSummaryAsync(vendorId, from.Value, to.Value);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET api/vendor-performance/favorites
        /// Returns each listing that has at least one favorite, with count.
        /// </summary>
        [HttpGet("favorites")]
        [ProducesResponseType(typeof(IReadOnlyList<FavoritedListingDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFavorites()
        {
            try
            {
                var vendorId = await ResolveVendorIdAsync();
                var result = await _service.GetFavoritedListingsAsync(vendorId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET api/vendor-performance/ai-suggestions?listingId={optional int}
        /// Returns AI suggestions grouped by listing, optionally filtered by listingId.
        /// </summary>
        [HttpGet("ai-suggestions")]
        [ProducesResponseType(typeof(AiSuggestionLogDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAiSuggestions([FromQuery] int? listingId = null)
        {
            try
            {
                var vendorId = await ResolveVendorIdAsync();
                var result = await _service.GetAiSuggestionLogAsync(vendorId, listingId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        private async Task<int> ResolveVendorIdAsync()
        {
            var vendorIdClaim = User.FindFirst("vendorId")?.Value
                ?? User.FindFirst("VendorId")?.Value
                ?? User.FindFirst("vendor_id")?.Value;

            if (int.TryParse(vendorIdClaim, out var vendorId))
            {
                return vendorId;
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("userId")?.Value
                ?? User.FindFirst("id")?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                return await _service.GetVendorIdAsync(userId);
            }

            throw new UnauthorizedAccessException("The authenticated user ID is invalid.");
        }
    }
}
