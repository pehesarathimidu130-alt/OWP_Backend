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
        private readonly Backend.Services.IFileStorage _fileStorage;

        public InquiriesController(AppDbContext context, Backend.Services.IFileStorage fileStorage)
        {
            _context = context;
            _fileStorage = fileStorage;
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
        public class CreateInquiryRequest
        {
            public int? VendorId { get; set; }
            public int? ServiceId { get; set; }
            public DateTime? WeddingDate { get; set; }
            public int? GuestCount { get; set; }
            public decimal? Budget { get; set; }
            public string? Message { get; set; }
            public IFormFile? Photo { get; set; }
            public IFormFile? Attachment { get; set; }
            public IFormFile? Image { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CreateInquiry([FromForm] CreateInquiryRequest request)
        {
            var vendorId = request.VendorId;
            var serviceId = request.ServiceId;
            var weddingDate = request.WeddingDate;
            var guestCount = request.GuestCount;
            var budget = request.Budget;
            var message = request.Message;
            var photo = request.Photo;
            var attachment = request.Attachment;
            var image = request.Image;
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
                    var extension = Path.GetExtension(file.FileName);
                    var uniqueFileName = $"inquiry_{Guid.NewGuid():N}{extension}";
                    attachmentUrl = await _fileStorage.SaveAsync(file, "inquiries", uniqueFileName, file.ContentType);
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
                    vendorReply = i.VendorReply,
                    repliedAt = i.RepliedAt,
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

        private async Task<int?> GetCurrentVendorIdAsync()
        {
            var vendorClaim = User.FindFirst("vendorId")?.Value
                ?? User.FindFirst("VendorId")?.Value
                ?? User.FindFirst("vendor_id")?.Value;

            if (int.TryParse(vendorClaim, out var vId) && vId > 0)
            {
                return vId;
            }

            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.UserId == userId.Value);
                if (vendor != null) return vendor.VendorId;
            }

            return null;
        }

        /// <summary>
        /// GET /api/inquiries/vendor/my-inquiries
        /// Returns all customer inquiries submitted to the authenticated vendor.
        /// </summary>
        [HttpGet("vendor/my-inquiries")]
        [HttpGet("vendor")]
        [Authorize(Roles = "Vendor,Admin,SuperAdmin")]
        public async Task<IActionResult> GetVendorInquiries()
        {
            var callerVendorId = await GetCurrentVendorIdAsync();
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

            if (!callerVendorId.HasValue && !isAdmin)
            {
                return Unauthorized(new { message = "Vendor profile not found for this account." });
            }

            var query = _context.VendorInquiries
                .Include(i => i.Customer)
                .Include(i => i.User)
                .Include(i => i.Vendor)
                .Include(i => i.VendorService!)
                    .ThenInclude(s => s.Category)
                .AsQueryable();

            if (callerVendorId.HasValue)
            {
                query = query.Where(i => i.VendorId == callerVendorId.Value);
            }

            var inquiries = await query
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new Backend.DTOs.VendorInquiryResponseDto
                {
                    InquiryId = i.InquiryId,
                    VendorId = i.VendorId,
                    VendorName = i.Vendor != null ? i.Vendor.BusinessName : "Wedding Vendor",
                    ServiceId = i.ServiceId,
                    ServiceName = i.VendorService != null ? i.VendorService.ServiceName : "General Vendor Inquiry",
                    ServiceImage = i.VendorService != null ? i.VendorService.CoverImageUrl : null,
                    CategoryName = i.VendorService != null && i.VendorService.Category != null
                        ? i.VendorService.Category.CategoryName
                        : (i.Vendor != null ? i.Vendor.Category : null),
                    CustomerId = i.CustomerId,
                    UserId = i.UserId,
                    CustomerName = i.Customer != null
                        ? ($"{i.Customer.FirstName} {i.Customer.LastName}").Trim()
                        : (i.User != null ? i.User.FullName : "Prospective Client"),
                    CustomerEmail = i.User != null ? i.User.Email : (i.Customer != null && i.Customer.User != null ? i.Customer.User.Email : ""),
                    CustomerPhone = i.User != null && !string.IsNullOrEmpty(i.User.PhoneNumber)
                        ? i.User.PhoneNumber
                        : (i.Customer != null && i.Customer.User != null ? i.Customer.User.PhoneNumber ?? "" : ""),
                    CustomerAvatar = i.Customer != null ? i.Customer.ProfilePhotoUrl : null,
                    WeddingDate = i.WeddingDate,
                    GuestCount = i.GuestCount,
                    Budget = i.Budget,
                    Message = i.Message,
                    AttachmentUrl = i.AttachmentUrl,
                    Status = i.Status,
                    VendorReply = i.VendorReply,
                    RepliedAt = i.RepliedAt,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt
                })
                .ToListAsync();

            return Ok(inquiries);
        }

        /// <summary>
        /// PATCH /api/inquiries/{id}/reply or POST /api/inquiries/{id}/reply
        /// Allows the authenticated vendor to reply to a customer inquiry and marks status as 'Replied'.
        /// </summary>
        [HttpPatch("{id:int}/reply")]
        [HttpPost("{id:int}/reply")]
        [Authorize(Roles = "Vendor,Admin,SuperAdmin")]
        public async Task<IActionResult> ReplyToInquiry(int id, [FromBody] Backend.DTOs.ReplyInquiryRequestDto request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Reply payload is required." });
            }

            var replyText = request.GetEffectiveMessage();
            if (string.IsNullOrWhiteSpace(replyText))
            {
                return BadRequest(new { message = "Reply message cannot be empty." });
            }

            var inquiry = await _context.VendorInquiries
                .Include(i => i.Vendor)
                .Include(i => i.Customer)
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.InquiryId == id);

            if (inquiry == null)
            {
                return NotFound(new { message = $"Inquiry with ID {id} was not found." });
            }

            var callerVendorId = await GetCurrentVendorIdAsync();
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

            if (!isAdmin && (!callerVendorId.HasValue || inquiry.VendorId != callerVendorId.Value))
            {
                return Forbid();
            }

            inquiry.VendorReply = replyText;
            inquiry.RepliedAt = DateTime.UtcNow;
            inquiry.Status = "Replied";
            inquiry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Trigger in-app notification to the customer
            if (inquiry.UserId.HasValue)
            {
                try
                {
                    var vendorName = inquiry.Vendor?.BusinessName ?? "Vendor";
                    var snippet = replyText.Length > 80 ? replyText.Substring(0, 77) + "..." : replyText;
                    _context.Notifications.Add(new Notification
                    {
                        UserId = inquiry.UserId.Value,
                        Title = "New Reply to Your Inquiry",
                        Message = $"{vendorName} replied: \"{snippet}\"",
                        Type = Backend.Constants.NotificationTypes.InquiryStatusChanged,
                        IsRead = false
                    });
                    await _context.SaveChangesAsync();
                }
                catch (Exception notifEx)
                {
                    Console.WriteLine($"Error sending inquiry reply notification: {notifEx.Message}");
                }
            }

            return Ok(new
            {
                success = true,
                message = "Reply sent successfully.",
                inquiryId = inquiry.InquiryId,
                status = inquiry.Status,
                vendorReply = inquiry.VendorReply,
                repliedAt = inquiry.RepliedAt
            });
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

            if (!string.IsNullOrEmpty(inquiry.AttachmentUrl))
            {
                await _fileStorage.DeleteAsync(inquiry.AttachmentUrl);
            }

            _context.VendorInquiries.Remove(inquiry);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Inquiry deleted successfully." });
        }

        /// <summary>
        /// PATCH /api/inquiries/{id}/status
        /// Updates an inquiry status. Allowed only for the owning vendor or an admin.
        /// </summary>
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateInquiryStatus(int id, [FromBody] UpdateInquiryStatusDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new { message = "Status is required." });
            }

            var allowedStatuses = new[] { "Pending", "Responded", "Replied" };
            var matchedStatus = allowedStatuses.FirstOrDefault(s => string.Equals(s, request.Status.Trim(), StringComparison.OrdinalIgnoreCase));
            if (matchedStatus == null)
            {
                return BadRequest(new { message = $"Invalid status '{request.Status}'. Allowed statuses are: {string.Join(", ", allowedStatuses)}." });
            }

            var inquiry = await _context.VendorInquiries
                .Include(i => i.Vendor)
                .FirstOrDefaultAsync(i => i.InquiryId == id);

            if (inquiry == null)
            {
                return NotFound(new { message = $"Inquiry with ID {id} was not found." });
            }

            var callerUserId = GetCurrentUserId();
            int? callerVendorId = null;
            var vendorClaim = User.FindFirst("vendorId")?.Value ?? User.FindFirst("VendorId")?.Value;
            if (int.TryParse(vendorClaim, out var vId))
            {
                callerVendorId = vId;
            }
            else if (callerUserId.HasValue)
            {
                var v = await _context.Vendors.FirstOrDefaultAsync(x => x.UserId == callerUserId.Value);
                if (v != null) callerVendorId = v.VendorId;
            }

            bool isAdmin = User.IsInRole("Admin") ||
                           string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin && (!callerVendorId.HasValue || inquiry.VendorId != callerVendorId.Value))
            {
                return Forbid();
            }

            var oldStatus = inquiry.Status;
            inquiry.Status = matchedStatus;
            inquiry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Customer notification on status change (if opted in)
            if (!string.Equals(oldStatus, matchedStatus, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    bool shouldNotify = true;
                    if (inquiry.CustomerId.HasValue)
                    {
                        var pref = await _context.CustomerNotificationPreferences
                            .FirstOrDefaultAsync(p => p.CustomerId == inquiry.CustomerId.Value);
                        if (pref != null)
                        {
                            shouldNotify = pref.InquiryUpdates;
                        }
                    }
                    else if (inquiry.UserId.HasValue)
                    {
                        var cust = await _context.Customers
                            .FirstOrDefaultAsync(c => c.UserId == inquiry.UserId.Value);
                        if (cust != null)
                        {
                            var pref = await _context.CustomerNotificationPreferences
                                .FirstOrDefaultAsync(p => p.CustomerId == cust.CustomerId);
                            if (pref != null)
                            {
                                shouldNotify = pref.InquiryUpdates;
                            }
                        }
                    }

                    if (shouldNotify && inquiry.UserId.HasValue)
                    {
                        var vendorName = inquiry.Vendor?.BusinessName ?? "Vendor";
                        _context.Notifications.Add(new Notification
                        {
                            UserId = inquiry.UserId.Value,
                            Title = "Inquiry Status Updated",
                            Message = $"Your inquiry with {vendorName} has been marked as {matchedStatus}.",
                            Type = Backend.Constants.NotificationTypes.InquiryStatusChanged,
                            IsRead = false
                        });
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating inquiry status change notification: {ex.Message}");
                }
            }

            return Ok(new
            {
                success = true,
                inquiryId = inquiry.InquiryId,
                status = inquiry.Status,
                message = "Inquiry status updated successfully."
            });
        }
    }

    public class UpdateInquiryStatusDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateInquiryDto
    {
        public DateTime? WeddingDate { get; set; }
        public int? GuestCount { get; set; }
        public decimal? Budget { get; set; }
        public string? Message { get; set; }
    }
}
