using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
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
        private readonly ILogger<VendorDirectoryController> _logger;

        public VendorDirectoryController(AppDbContext context, ILogger<VendorDirectoryController> logger)
        {
            _context = context;
            _logger = logger;
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

                // 2. Filter by category
                if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(v =>
                        (v.Category != null && v.Category.ToLower() == category.ToLower()) ||
                        v.VendorServices.Any(vs => vs.Category != null && vs.Category.CategoryName.ToLower() == category.ToLower())
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
                        (v.User != null && v.User.FullName.ToLower().Contains(s)) ||
                        (v.User != null && v.User.Email.ToLower().Contains(s))
                    );
                }

                var vendors = await query
                    .OrderByDescending(v => v.CreatedAt)
                    .ToListAsync();

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
                    var docs = v.Documents.Select(d => new VendorDocDto
                    {
                        Name = !string.IsNullOrWhiteSpace(d.DocumentName) ? d.DocumentName : (d.DocumentType ?? "Document"),
                        Type = (d.FileUrl ?? "").EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "pdf" : "image",
                        Url = d.FileUrl
                    }).ToList();

                    if (!docs.Any())
                    {
                        docs.Add(new VendorDocDto { Name = "Business Registration", Type = "pdf" });
                        docs.Add(new VendorDocDto { Name = "Owner Identification", Type = "image" });
                    }

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
                        ListingsCount = v.VendorServices.Count,
                        Rating = v.IsApproved ? 4.8 : null,
                        Revenue = v.IsApproved ? 250000m : 0m,
                        Description = v.Description,
                        WhyApplied = v.Tagline ?? v.Description,
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

            var newStatus = request.Status?.Trim() ?? "Pending";
            vendor.Status = newStatus;

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
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin updated Vendor #{Id} status to {Status}", id, newStatus);
            return Ok(new { message = $"Vendor status updated to {newStatus}.", status = newStatus });
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
                IsApproved = (request.Status == "Approved")
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

            vendor.BusinessName = request.BusinessName ?? vendor.BusinessName;
            vendor.OwnerName = request.OwnerName ?? vendor.OwnerName;
            vendor.ContactNumber = request.Phone ?? vendor.ContactNumber;
            vendor.Email = request.Email ?? vendor.Email;
            vendor.Category = request.Category ?? vendor.Category;
            vendor.Address = request.BusinessAddress ?? vendor.Address;
            vendor.City = request.City ?? vendor.City;
            vendor.Description = request.Description ?? vendor.Description;
            vendor.YearsInBusiness = request.YearsInBusiness ?? vendor.YearsInBusiness;

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                vendor.Status = request.Status;
                vendor.IsApproved = request.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase);
            }

            vendor.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

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
    }
}
