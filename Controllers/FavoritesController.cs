using Backend.Data;
using Backend.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FavoritesController> _logger;

        public FavoritesController(AppDbContext context, ILogger<FavoritesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("userId")?.Value
                ?? User.FindFirst("id")?.Value;

            if (int.TryParse(claim, out int userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// GET /api/favorites
        /// Returns all favorited listings for the authenticated user with parent vendor info.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFavoriteListings()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Invalid user identification in token." });
            }

            try
            {
                var favoriteItems = await _context.CustomerFavorites
                    .Where(cf => cf.UserId == userId.Value)
                    .Include(cf => cf.VendorService)
                        .ThenInclude(vs => vs!.Vendor)
                    .Include(cf => cf.VendorService)
                        .ThenInclude(vs => vs!.Category)
                    .Include(cf => cf.VendorService)
                        .ThenInclude(vs => vs!.Images)
                    .OrderByDescending(cf => cf.CreatedAt)
                    .ToListAsync();

                var fallbackImage = "https://images.unsplash.com/photo-1519741497674-611481863552?auto=format&fit=crop&w=800&q=80";

                var result = favoriteItems
                    .Where(cf => cf.VendorService != null)
                    .Select(cf =>
                    {
                        var vs = cf.VendorService!;
                        var categoryName = vs.Category?.CategoryName ?? "General";

                        return new
                        {
                            id = vs.ServiceId.ToString(),
                            serviceId = vs.ServiceId,
                            title = vs.ServiceName,
                            serviceName = vs.ServiceName,
                            category = categoryName,
                            categoryId = vs.CategoryId,
                            categoryIcon = GetCategoryIcon(categoryName),
                            shortDescription = vs.ShortDescription,
                            description = !string.IsNullOrWhiteSpace(vs.Description) ? vs.Description : vs.ShortDescription,
                            price = vs.Price,
                            priceFrom = (double)(vs.Price ?? 0),
                            isPriceOnRequest = vs.IsPriceOnRequest,
                            status = vs.Status,
                            coverImageUrl = vs.CoverImageUrl ?? vs.Vendor?.CoverImageUrl ?? fallbackImage,
                            imageUrl = vs.CoverImageUrl ?? vs.Vendor?.CoverImageUrl ?? fallbackImage,
                            images = vs.Images.Select(img => img.ImageUrl).ToList(),
                            isFavorite = true,

                            // Parent Vendor info
                            vendorId = vs.VendorId,
                            vendor = new
                            {
                                id = vs.Vendor?.VendorId.ToString() ?? vs.VendorId.ToString(),
                                vendorId = vs.VendorId,
                                name = vs.Vendor?.BusinessName ?? "Verified Vendor",
                                businessName = vs.Vendor?.BusinessName ?? "Verified Vendor",
                                ownerName = vs.Vendor?.OwnerName,
                                location = vs.Vendor?.City ?? vs.Vendor?.Address ?? "Sri Lanka",
                                city = vs.Vendor?.City ?? "Sri Lanka",
                                address = vs.Vendor?.Address,
                                contactNumber = vs.Vendor?.ContactNumber,
                                email = vs.Vendor?.Email,
                                logoUrl = vs.Vendor?.LogoUrl,
                                coverImageUrl = vs.Vendor?.CoverImageUrl,
                                rating = 4.9,
                                reviewCount = 18,
                                yearsInBusiness = vs.Vendor?.YearsInBusiness ?? 3,
                                isApproved = vs.Vendor?.IsApproved ?? true
                            }
                        };
                    }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching favorites for user {UserId}", userId);
                return Problem(
                    detail: "An error occurred while fetching your favorite listings.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        /// <summary>
        /// GET /api/favorites/ids
        /// Returns an integer array of all service IDs favorited by the authenticated user.
        /// </summary>
        [HttpGet("ids")]
        public async Task<IActionResult> GetFavoriteIds()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Invalid user identification in token." });
            }

            try
            {
                var ids = await _context.CustomerFavorites
                    .Where(cf => cf.UserId == userId.Value)
                    .Select(cf => cf.ServiceId)
                    .ToListAsync();

                return Ok(ids);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching favorite IDs for user {UserId}", userId);
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// POST /api/favorites/toggle/{listingId}
        /// Or POST /api/favorites/{listingId}
        /// Toggles a listing in the user's favorites list.
        /// </summary>
        [HttpPost("toggle/{listingId:int}")]
        [HttpPost("{listingId:int}")]
        public async Task<IActionResult> ToggleFavorite([FromRoute] int listingId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Invalid user identification in token." });
            }

            // Check if listing exists
            var serviceExists = await _context.VendorServices.AnyAsync(vs => vs.ServiceId == listingId);
            if (!serviceExists)
            {
                return NotFound(new { message = $"Service with ID {listingId} not found." });
            }

            try
            {
                var existing = await _context.CustomerFavorites
                    .FirstOrDefaultAsync(cf => cf.UserId == userId.Value && cf.ServiceId == listingId);

                if (existing != null)
                {
                    // Remove from favorites
                    _context.CustomerFavorites.Remove(existing);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        isFavorite = false,
                        listingId = listingId,
                        message = "Removed from favourites"
                    });
                }
                else
                {
                    // Add to favorites
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId.Value);

                    var newFav = new CustomerFavorite
                    {
                        UserId = userId.Value,
                        CustomerId = customer?.CustomerId,
                        ServiceId = listingId
                    };

                    _context.CustomerFavorites.Add(newFav);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        isFavorite = true,
                        listingId = listingId,
                        message = "Added to favourites"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite for user {UserId} and service {ServiceId}", userId, listingId);
                return Problem(
                    detail: "An error occurred while updating your favourites.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        /// <summary>
        /// DELETE /api/favorites/{listingId}
        /// Explicitly removes a listing from favorites.
        /// </summary>
        [HttpDelete("{listingId:int}")]
        public async Task<IActionResult> RemoveFavorite([FromRoute] int listingId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Invalid user identification in token." });
            }

            try
            {
                var existing = await _context.CustomerFavorites
                    .FirstOrDefaultAsync(cf => cf.UserId == userId.Value && cf.ServiceId == listingId);

                if (existing != null)
                {
                    _context.CustomerFavorites.Remove(existing);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    isFavorite = false,
                    listingId = listingId,
                    message = "Removed from favourites"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing favorite for user {UserId} and service {ServiceId}", userId, listingId);
                return Problem(
                    detail: "An error occurred while removing from favourites.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        private static string GetCategoryIcon(string category)
        {
            return category.ToLower() switch
            {
                "hotel / venue" => "castle_rounded",
                "photography" => "photo_camera_rounded",
                "decorations" => "yard_rounded",
                "catering" => "restaurant_menu_rounded",
                "music" => "music_note_rounded",
                _ => "category_rounded"
            };
        }
    }
}
