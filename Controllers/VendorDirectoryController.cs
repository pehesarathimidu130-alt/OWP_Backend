using Backend.Constants;
using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    /// <summary>
    /// VendorDirectoryController — Provides Admin and SuperAdmin endpoints
    /// to browse, search, filter, update, approve/reject/suspend, and manage vendors.
    /// Route prefix: /api/admin/vendors
    /// </summary>
    [ApiController]
    [Route("api/admin/vendors")]
    [Authorize(Roles = "Admin,SuperAdmin,ADMIN,SUPER_ADMIN,admin,superadmin")]
    public class VendorDirectoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLogService;
        private readonly ILogger<VendorDirectoryController> _logger;
        private readonly IWebHostEnvironment _environment;

        public VendorDirectoryController(
            AppDbContext context,
            IActivityLogService activityLogService,
            ILogger<VendorDirectoryController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _activityLogService = activityLogService;
            _logger = logger;
            _environment = environment;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/vendors
        // Returns a filtered list of all vendors with listing counts and details.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        [ProducesResponseType(typeof(List<VendorSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVendors(
            [FromQuery] string? status,
            [FromQuery] string? category,
            [FromQuery] string? search)
        {
            try
            {
                var query = _context.Vendors
                    .Include(v => v.User)
                    .Include(v => v.VendorServices)
                        .ThenInclude(vs => vs.Category)
                    .Include(v => v.Documents)
                    .AsQueryable();

                // 1. Filter by status
                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    if (status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(v => v.Status == "Approved" || v.Status == "Active" || v.IsApproved);
                    }
                    else if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(v => v.Status == "Pending" || (!v.IsApproved && v.Status != "Rejected" && v.Status != "Suspended" && v.Status != "Banned"));
                    }
                    else if (status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(v => v.Status == "Rejected" || v.Status == "Inactive");
                    }
                    else
                    {
                        query = query.Where(v => v.Status == status);
                    }
                }

                // 2. Filter by category (supports CSV multi-category membership)
                if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    var catLower = category.Trim().ToLower();
                    query = query.Where(v =>
                        (v.Category != null && v.Category.ToLower().Contains(catLower)) ||
                        v.VendorServices.Any(vs => vs.Category != null && vs.Category.CategoryName.ToLower() == catLower)
                    );
                }

                // 3. Search query
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim().ToLower();
                    query = query.Where(v =>
                        v.BusinessName.ToLower().Contains(s) ||
                        (v.OwnerName != null && v.OwnerName.ToLower().Contains(s)) ||
                        (v.Email != null && v.Email.ToLower().Contains(s)) ||
                        (v.City != null && v.City.ToLower().Contains(s)) ||
                        (v.Category != null && v.Category.ToLower().Contains(s)) ||
                        (v.User != null && v.User.FullName.ToLower().Contains(s)) ||
                        (v.User != null && v.User.Email.ToLower().Contains(s))
                    );
                }

                var vendors = await query
                    .OrderByDescending(v => v.CreatedAt)
                    .ToListAsync();

                // Post-query exact token membership check for CSV categories
                if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    var catTrimmed = category.Trim();
                    vendors = vendors.Where(v =>
                        (v.Category != null && v.Category
                            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                            .Any(c => c.Equals(catTrimmed, StringComparison.OrdinalIgnoreCase))) ||
                        v.VendorServices.Any(vs => vs.Category != null && vs.Category.CategoryName.Equals(catTrimmed, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                var result = vendors.Select(v =>
                {
                    var normalizedStatus = v.Status switch
                    {
                        "Active" => "Approved",
                        "Inactive" => "Rejected",
                        _ => v.Status
                    };

                    if (v.IsApproved && normalizedStatus == "Pending")
                    {
                        normalizedStatus = "Approved";
                    }

                    var primaryCategory = !string.IsNullOrWhiteSpace(v.Category)
                        ? v.Category
                        : v.VendorServices.FirstOrDefault(vs => vs.Category != null)?.Category?.CategoryName ?? "General";

                    // Build documents list
                    var docs = v.Documents.Select(d =>
                    {
                        var rawFileName = !string.IsNullOrWhiteSpace(d.FileUrl) 
                            ? Path.GetFileName(d.FileUrl) 
                            : string.Empty;

                        return new VendorDocDto
                        {
                            DocumentId = d.DocumentId,
                            Name = !string.IsNullOrWhiteSpace(d.DocumentName) ? d.DocumentName : (d.DocumentType ?? "Document"),
                            DocumentType = !string.IsNullOrWhiteSpace(d.DocumentType) ? d.DocumentType : "Business Registration",
                            FileName = !string.IsNullOrWhiteSpace(rawFileName) ? rawFileName : (!string.IsNullOrWhiteSpace(d.DocumentName) ? d.DocumentName : "document"),
                            Type = (d.FileUrl ?? "").EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "pdf" : "image",
                            Url = d.FileUrl,
                            UploadDate = d.CreatedAt.ToString("yyyy-MM-dd"),
                            Status = d.Status ?? "Pending"
                        };
                    }).ToList();

                    return new VendorSummaryDto
                    {
                        Id = $"#{v.VendorId}",
                        VendorId = v.VendorId,
                        BusinessName = v.BusinessName,
                        OwnerName = !string.IsNullOrWhiteSpace(v.OwnerName) ? v.OwnerName : (v.User?.FullName ?? "Unknown"),
                        Email = !string.IsNullOrWhiteSpace(v.Email) ? v.Email : (v.User?.Email ?? string.Empty),
                        Phone = !string.IsNullOrWhiteSpace(v.ContactNumber) ? v.ContactNumber : (v.User?.PhoneNumber ?? string.Empty),
                        Category = primaryCategory,
                        Status = normalizedStatus,
                        BusinessAddress = !string.IsNullOrWhiteSpace(v.Address) ? v.Address : v.City,
                        City = v.City,
                        TaxId = $"BR/{v.CreatedAt.Year:D4}/{v.VendorId:D3}",
                        RegistrationYear = v.CreatedAt.Year,
                        YearsInBusiness = v.YearsInBusiness ?? 5,
                        BusinessLicenseVerified = v.IsApproved,
                        AppliedDate = v.CreatedAt.ToString("yyyy-MM-dd"),
                        ApprovedDate = v.IsApproved ? v.UpdatedAt.ToString("yyyy-MM-dd") : null,
                        StatusChangeReason = v.StatusChangeReason,
                        StatusChangedAt = v.StatusChangedAt?.ToString("yyyy-MM-dd HH:mm"),
                        SuspendedDate = v.Status == "Suspended" ? v.StatusChangedAt?.ToString("yyyy-MM-dd") : null,
                        SuspendReason = v.Status == "Suspended" ? v.StatusChangeReason : null,
                        BanDate = v.Status == "Banned" ? v.StatusChangedAt?.ToString("yyyy-MM-dd") : null,
                        BanReason = v.Status == "Banned" ? v.StatusChangeReason : null,
                        RejectReason = v.Status == "Rejected" ? v.StatusChangeReason : null,
                        ListingsCount = v.VendorServices.Count,
                        Rating = v.IsApproved ? 4.8 : null,
                        Revenue = v.IsApproved ? 250000m : 0m,
                        Description = v.Description,
                        WhyApplied = v.Tagline ?? v.Description,
                        LogoUrl = v.LogoUrl,
                        CoverImageUrl = v.CoverImageUrl,
                        ImageUrl = !string.IsNullOrWhiteSpace(v.LogoUrl) 
                            ? (v.LogoUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? v.LogoUrl : $"http://localhost:5131{v.LogoUrl}")
                            : (!string.IsNullOrWhiteSpace(v.CoverImageUrl) 
                                ? (v.CoverImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? v.CoverImageUrl : $"http://localhost:5131{v.CoverImageUrl}") 
                                : null),
                        VerificationDocs = docs
                    };
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching vendors for directory.");
                return StatusCode(500, new ProblemDetails { Detail = "Failed to retrieve vendors.", Title = "Server Error" });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/admin/vendors/{id}/status
        // Updates vendor approval and operational status (Approved/Rejected/Suspended/Banned)
        // ─────────────────────────────────────────────────────────────────────
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateVendorStatusDto request)
        {
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == id);
            if (vendor == null)
            {
                return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Vendor #{id} not found." });
            }

            var oldStatus = vendor.Status;
            var newStatus = request.Status?.Trim() ?? "Pending";
            vendor.Status = newStatus;
            vendor.StatusChangeReason = request.Reason?.Trim();
            vendor.StatusChangedAt = DateTime.UtcNow;

            if (newStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                vendor.IsApproved = true;
                vendor.VerificationStatus = "Verified";
            }
            else if (newStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase) ||
                     newStatus.Equals("Suspended", StringComparison.OrdinalIgnoreCase) ||
                     newStatus.Equals("Banned", StringComparison.OrdinalIgnoreCase))
            {
                vendor.IsApproved = false;
                if (newStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    vendor.VerificationStatus = "Rejected";
                }
            }

            vendor.UpdatedAt = DateTime.UtcNow;

            var reasonText = !string.IsNullOrWhiteSpace(request.Reason) ? request.Reason.Trim() : "None provided";
            await _activityLogService.LogAsync(
                ActivityLogTypes.VendorStatusChanged,
                "Vendor",
                vendor.VendorId.ToString(),
                $"Status updated from '{oldStatus}' to '{newStatus}' for vendor '{vendor.BusinessName}'. Reason: {reasonText}",
                saveChanges: false);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin updated Vendor #{Id} status to {Status}. Reason: {Reason}", id, newStatus, request.Reason);
            return Ok(new { 
                message = $"Vendor status updated to {newStatus}.", 
                status = newStatus,
                statusChangeReason = vendor.StatusChangeReason,
                statusChangedAt = vendor.StatusChangedAt?.ToString("yyyy-MM-dd HH:mm")
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/admin/vendors
        // Creates a new vendor in the directory
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CreateVendor([FromBody] SaveVendorDto request)
        {
            if (string.IsNullOrWhiteSpace(request.BusinessName))
            {
                return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "Business Name is required." });
            }

            // Look for default vendor role
            var vendorRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Vendor");
            var roleId = vendorRole?.RoleId ?? 3;

            // Check or create associated user
            var email = !string.IsNullOrWhiteSpace(request.Email) ? request.Email.Trim() : $"vendor_{Guid.NewGuid().ToString("N").Substring(0, 6)}@oleena.com";
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            int userId;

            if (existingUser != null)
            {
                userId = existingUser.UserId;
            }
            else
            {
                var newUser = new User
                {
                    Email = email,
                    FullName = request.OwnerName ?? request.BusinessName,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Vendor@123"),
                    RoleId = roleId,
                    PhoneNumber = request.Phone,
                    IsActive = true
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                userId = newUser.UserId;
            }

            var vendor = new Vendor
            {
                UserId = userId,
                BusinessName = request.BusinessName.Trim(),
                OwnerName = request.OwnerName,
                Email = email,
                ContactNumber = request.Phone,
                Category = request.Category ?? "Hotels",
                Address = request.BusinessAddress,
                City = request.City ?? "Colombo",
                Description = request.Description,
                YearsInBusiness = request.YearsInBusiness,
                Status = request.Status ?? "Pending",
                IsApproved = (request.Status == "Approved"),
                StatusChangedAt = DateTime.UtcNow
            };

            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVendors), new { id = vendor.VendorId }, new { vendorId = vendor.VendorId, message = "Vendor created successfully." });
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/admin/vendors/{id}
        // Updates vendor profile details
        // ─────────────────────────────────────────────────────────────────────
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateVendor(int id, [FromBody] SaveVendorDto request)
        {
            var vendor = await _context.Vendors.Include(v => v.User).FirstOrDefaultAsync(v => v.VendorId == id);
            if (vendor == null)
            {
                return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Vendor #{id} not found." });
            }

            var changedFields = new List<string>();
            void CheckField(string name, string? oldVal, string? newVal)
            {
                if (newVal != null && !string.Equals(newVal.Trim(), (oldVal ?? "").Trim(), StringComparison.Ordinal))
                {
                    changedFields.Add($"{name}: '{oldVal}' -> '{newVal.Trim()}'");
                }
            }

            CheckField("BusinessName", vendor.BusinessName, request.BusinessName);
            CheckField("OwnerName", vendor.OwnerName, request.OwnerName);
            CheckField("Phone", vendor.ContactNumber, request.Phone);
            CheckField("Email", vendor.Email, request.Email);
            CheckField("Category", vendor.Category, request.Category);
            CheckField("Address", vendor.Address, request.BusinessAddress);
            CheckField("City", vendor.City, request.City);
            CheckField("Description", vendor.Description, request.Description);
            if (request.YearsInBusiness.HasValue && request.YearsInBusiness != vendor.YearsInBusiness)
            {
                changedFields.Add($"YearsInBusiness: '{vendor.YearsInBusiness}' -> '{request.YearsInBusiness}'");
            }

            vendor.BusinessName = request.BusinessName ?? vendor.BusinessName;
            vendor.OwnerName = request.OwnerName ?? vendor.OwnerName;
            vendor.ContactNumber = request.Phone ?? vendor.ContactNumber;
            vendor.Email = request.Email ?? vendor.Email;
            vendor.Category = request.Category ?? vendor.Category;
            vendor.Address = request.BusinessAddress ?? vendor.Address;
            vendor.City = request.City ?? vendor.City;
            vendor.Description = request.Description ?? vendor.Description;
            vendor.YearsInBusiness = request.YearsInBusiness ?? vendor.YearsInBusiness;

            if (vendor.User != null)
            {
                if (!string.IsNullOrWhiteSpace(request.OwnerName)) vendor.User.FullName = request.OwnerName.Trim();
                if (!string.IsNullOrWhiteSpace(request.Email)) vendor.User.Email = request.Email.Trim();
                if (!string.IsNullOrWhiteSpace(request.Phone)) vendor.User.PhoneNumber = request.Phone.Trim();
                vendor.User.UpdatedAt = DateTime.UtcNow;
            }

            if (!string.IsNullOrWhiteSpace(request.Reason))
            {
                vendor.StatusChangeReason = request.Reason.Trim();
                vendor.StatusChangedAt = DateTime.UtcNow;
            }

            if (!string.IsNullOrWhiteSpace(request.Status) && vendor.Status != request.Status)
            {
                var oldStatus = vendor.Status;
                vendor.Status = request.Status;
                vendor.IsApproved = request.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase);
                vendor.StatusChangedAt = DateTime.UtcNow;

                await _activityLogService.LogAsync(
                    ActivityLogTypes.VendorStatusChanged,
                    "Vendor",
                    vendor.VendorId.ToString(),
                    $"Status updated from '{oldStatus}' to '{request.Status}' for vendor '{vendor.BusinessName}'. Reason: {request.Reason ?? "None provided"}",
                    saveChanges: false);
            }

            if (changedFields.Count > 0)
            {
                var reasonSuffix = !string.IsNullOrWhiteSpace(request.Reason) ? $" | Reason: {request.Reason.Trim()}" : "";
                await _activityLogService.LogAsync(
                    ActivityLogTypes.VendorFieldEdited,
                    "Vendor",
                    vendor.VendorId.ToString(),
                    $"Edited vendor fields for '{vendor.BusinessName}': {string.Join("; ", changedFields)}{reasonSuffix}",
                    saveChanges: false);
            }

            vendor.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin updated Vendor #{Id} ({BusinessName}). Reason: {Reason}", id, vendor.BusinessName, request.Reason ?? "None provided");
            return Ok(new { message = "Vendor updated successfully.", vendorId = id });
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE /api/admin/vendors/{id}
        // Deletes a vendor from the directory
        // ─────────────────────────────────────────────────────────────────────
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == id);
            if (vendor == null)
            {
                return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Vendor #{id} not found." });
            }

            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin deleted Vendor #{Id}", id);
            return Ok(new { message = $"Vendor #{id} deleted successfully." });
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE /api/admin/vendors/{id}/image
        // Clears the vendor's image (LogoUrl and CoverImageUrl) and removes physical files
        // ─────────────────────────────────────────────────────────────────────
        [HttpDelete("{id:int}/image")]
        [HttpDelete("{id:int}/logo")]
        public async Task<IActionResult> RemoveVendorImage(int id)
        {
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == id);
            if (vendor == null)
            {
                return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Vendor #{id} not found." });
            }

            DeletePhysicalFile(vendor.LogoUrl);
            DeletePhysicalFile(vendor.CoverImageUrl);

            vendor.LogoUrl = null;
            vendor.CoverImageUrl = null;
            vendor.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin removed image for Vendor #{Id}", id);
            return Ok(new { message = $"Image removed successfully for Vendor #{id}." });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/vendors/{id}/documents
        // Returns the list of uploaded verification documents for a vendor
        // Scoped to authorized admin roles via controller-level [Authorize]
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{id:int}/documents")]
        [ProducesResponseType(typeof(List<VendorDocDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVendorDocuments(int id)
        {
            var vendorExists = await _context.Vendors.AnyAsync(v => v.VendorId == id);
            if (!vendorExists)
            {
                return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Vendor #{id} not found." });
            }

            var documents = await _context.VendorDocuments
                .AsNoTracking()
                .Where(d => d.VendorId == id)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new VendorDocDto
                {
                    DocumentId = d.DocumentId,
                    Name = !string.IsNullOrWhiteSpace(d.DocumentName) ? d.DocumentName : (d.DocumentType ?? "Document"),
                    DocumentType = !string.IsNullOrWhiteSpace(d.DocumentType) ? d.DocumentType : "Business Registration",
                    FileName = !string.IsNullOrWhiteSpace(d.FileUrl) ? Path.GetFileName(d.FileUrl) : (!string.IsNullOrWhiteSpace(d.DocumentName) ? d.DocumentName : "document"),
                    Type = (d.FileUrl ?? "").EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "pdf" : "image",
                    Url = d.FileUrl,
                    UploadDate = d.CreatedAt.ToString("yyyy-MM-dd"),
                    Status = d.Status ?? "Pending"
                })
                .ToListAsync();

            return Ok(documents);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/vendors/{id}/documents/{documentId}/file
        // Streams the document file securely for authorized admin roles
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{id:int}/documents/{documentId:int}/file")]
        public async Task<IActionResult> GetDocumentFile(int id, int documentId)
        {
            var doc = await _context.VendorDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.VendorId == id);

            if (doc == null || string.IsNullOrWhiteSpace(doc.FileUrl))
            {
                return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Document not found." });
            }

            var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var cleanPath = doc.FileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(rootPath, cleanPath);

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Physical document file not found on server." });
            }

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, contentType, enableRangeProcessing: true);
        }

        private void DeletePhysicalFile(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl)) return;
            try
            {
                var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var cleanPath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(rootPath, cleanPath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete physical file {Path}", relativeUrl);
            }
        }
    }
}
