using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Backend.Data;
using Backend.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InquiriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public InquiriesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
        /// POST /api/inquiries
        /// Submits a new inquiry with optional photo attachment via multipart/form-data.
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data", "application/json")]
        public async Task<IActionResult> CreateInquiry(
            [FromForm] int? vendorId,
            [FromForm] int? serviceId,
            [FromForm] DateTime? weddingDate,
            [FromForm] int? guestCount,
            [FromForm] decimal? budget,
            [FromForm] string? message,
            [FromForm] IFormFile? photo,
            [FromForm] IFormFile? attachment,
            [FromForm] IFormFile? image)
        {
            var userId = GetCurrentUserId();
            var customer = userId.HasValue
                ? await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId.Value)
                : null;

            // Resolve vendor ID if only serviceId is provided
            int targetVendorId = vendorId ?? 0;
            if (targetVendorId == 0 && serviceId.HasValue && serviceId.Value > 0)
            {
                var service = await _context.VendorServices.FirstOrDefaultAsync(s => s.ServiceId == serviceId.Value);
                if (service != null)
                {
                    targetVendorId = service.VendorId;
                }
            }

            // Handle optional photo attachment
            string? attachmentUrl = null;
            var file = photo ?? attachment ?? image;
            if (file != null && file.Length > 0)
            {
                try
                {
                    var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadsDir = Path.Combine(webRoot, "uploads", "inquiries");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var extension = Path.GetExtension(file.FileName);
                    var uniqueFileName = $"inquiry_{Guid.NewGuid():N}{extension}";
                    var filePath = Path.Combine(uploadsDir, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    attachmentUrl = $"/uploads/inquiries/{uniqueFileName}";
                }
                catch (Exception ex)
                {
                    // Non-fatal: log and proceed with inquiry
                    Console.WriteLine($"Error uploading inquiry attachment: {ex.Message}");
                }
            }

            var inquiry = new VendorInquiry
            {
                UserId = userId,
                CustomerId = customer?.CustomerId,
                VendorId = targetVendorId > 0 ? targetVendorId : null,
                ServiceId = serviceId > 0 ? serviceId : null,
                WeddingDate = weddingDate.HasValue ? DateTime.SpecifyKind(weddingDate.Value, DateTimeKind.Utc) : null,
                GuestCount = guestCount,
                Budget = budget,
                Message = message?.Trim(),
                AttachmentUrl = attachmentUrl,
                Status = "Pending"
            };

            _context.VendorInquiries.Add(inquiry);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Your inquiry has been submitted successfully!",
                inquiryId = inquiry.InquiryId,
                attachmentUrl = inquiry.AttachmentUrl,
                status = inquiry.Status
            });
        }

        /// <summary>
        /// GET /api/inquiries or GET /api/inquiries/customer
        /// Returns all inquiries created by the authenticated customer.
        /// </summary>
        [HttpGet]
        [HttpGet("customer")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyInquiries()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not identified." });
            }

            var inquiries = await _context.VendorInquiries
                .Where(i => i.UserId == userId.Value)
                .Include(i => i.Vendor)
                .Include(i => i.VendorService)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    inquiryId = i.InquiryId,
                    vendorId = i.VendorId,
                    vendorName = i.Vendor != null ? i.Vendor.BusinessName : "Wedding Vendor",
                    vendorImage = i.Vendor != null ? (i.Vendor.CoverImageUrl ?? i.Vendor.LogoUrl) : null,
                    serviceId = i.ServiceId,
                    serviceName = i.VendorService != null ? i.VendorService.ServiceName : null,
                    weddingDate = i.WeddingDate,
                    guestCount = i.GuestCount,
                    budget = i.Budget,
                    message = i.Message,
                    attachmentUrl = i.AttachmentUrl,
                    status = i.Status,
                    createdAt = i.CreatedAt
                })
                .ToListAsync();

            return Ok(inquiries);
        }

        /// <summary>
        /// PUT /api/inquiries/{id}
        /// Allows user to edit their inquiry if still Pending.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInquiry(int id, [FromBody] UpdateInquiryDto req)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not identified." });
            }

            var inquiry = await _context.VendorInquiries.FirstOrDefaultAsync(i => i.InquiryId == id && i.UserId == userId.Value);
            if (inquiry == null)
            {
                return NotFound(new { message = "Inquiry not found." });
            }

            if (!string.Equals(inquiry.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Only Pending inquiries can be edited." });
            }

            if (req.WeddingDate.HasValue)
            {
                inquiry.WeddingDate = DateTime.SpecifyKind(req.WeddingDate.Value, DateTimeKind.Utc);
            }
            if (req.GuestCount.HasValue) inquiry.GuestCount = req.GuestCount.Value;
            if (req.Budget.HasValue) inquiry.Budget = req.Budget.Value;
            if (!string.IsNullOrWhiteSpace(req.Message)) inquiry.Message = req.Message.Trim();

            inquiry.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Inquiry updated successfully." });
        }

        /// <summary>
        /// DELETE /api/inquiries/{id}
        /// Deletes an inquiry.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInquiry(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not identified." });
            }

            var inquiry = await _context.VendorInquiries.FirstOrDefaultAsync(i => i.InquiryId == id && i.UserId == userId.Value);
            if (inquiry == null)
            {
                return NotFound(new { message = "Inquiry not found." });
            }

            _context.VendorInquiries.Remove(inquiry);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Inquiry deleted successfully." });
        }
    }

    public class UpdateInquiryDto
    {
        public DateTime? WeddingDate { get; set; }
        public int? GuestCount { get; set; }
        public decimal? Budget { get; set; }
        public string? Message { get; set; }
    }
}
