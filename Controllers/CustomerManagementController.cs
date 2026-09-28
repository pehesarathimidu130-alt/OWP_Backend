using Backend.Data;
using Backend.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/customers")]
    [Route("api/customer-management")]
    [Route("api/admin/customers")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,Admin,SuperAdmin,admin,superadmin")]
    public class CustomerManagementController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CustomerManagementController> _logger;

        public CustomerManagementController(AppDbContext context, ILogger<CustomerManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /api/customers or /api/customer-management
        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                // 1. Fetch from Customers table joined with Users
                var customersFromTable = await _context.Customers
                    .Include(c => c.User)
                    .Select(c => new
                    {
                        customerId = c.CustomerId,
                        id = c.CustomerId,
                        userId = c.UserId,
                        coupleName = ((c.FirstName ?? "") + " " + (c.LastName ?? "")).Trim(),
                        coupleNames = ((c.FirstName ?? "") + " " + (c.LastName ?? "")).Trim(),
                        firstName = c.FirstName ?? "",
                        lastName = c.LastName ?? "",
                        email = c.User != null ? c.User.Email : string.Empty,
                        phoneNumber = c.User != null ? c.User.PhoneNumber : null,
                        phone = c.User != null ? (c.User.PhoneNumber ?? "N/A") : "N/A",
                        status = c.User != null && c.User.IsActive ? "Active" : "Inactive",
                        joinDate = c.CreatedAt.ToString("MMM dd, yyyy"),
                        weddingDate = _context.VendorInquiries
                            .Where(vi => vi.CustomerId == c.CustomerId || vi.UserId == c.UserId)
                            .OrderByDescending(vi => vi.CreatedAt)
                            .Select(vi => vi.WeddingDate)
                            .FirstOrDefault() != null
                            ? _context.VendorInquiries
                                .Where(vi => vi.CustomerId == c.CustomerId || vi.UserId == c.UserId)
                                .OrderByDescending(vi => vi.CreatedAt)
                                .Select(vi => vi.WeddingDate!.Value.ToString("MMM dd, yyyy"))
                                .FirstOrDefault()
                            : "N/A",
                        inquiriesSent = _context.VendorInquiries
                            .Count(vi => vi.CustomerId == c.CustomerId || vi.UserId == c.UserId),
                        inquiriesCount = _context.VendorInquiries
                            .Count(vi => vi.CustomerId == c.CustomerId || vi.UserId == c.UserId)
                    })
                    .ToListAsync();

                // 2. Also include any registered mobile users with role Customer who might only be in Users table
                var existingUserIds = customersFromTable.Select(c => c.userId).ToHashSet();
                var customerUsers = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => !existingUserIds.Contains(u.UserId) && u.Role != null && u.Role.RoleName.ToLower().Contains("cust"))
                    .Select(u => new
                    {
                        customerId = u.UserId,
                        id = u.UserId,
                        userId = u.UserId,
                        coupleName = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : "Registered Couple",
                        coupleNames = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : "Registered Couple",
                        firstName = u.FullName,
                        lastName = "",
                        email = u.Email,
                        phoneNumber = u.PhoneNumber,
                        phone = !string.IsNullOrWhiteSpace(u.PhoneNumber) ? u.PhoneNumber : "N/A",
                        status = u.IsActive ? "Active" : "Inactive",
                        joinDate = u.CreatedAt.ToString("MMM dd, yyyy"),
                        weddingDate = _context.VendorInquiries
                            .Where(vi => vi.UserId == u.UserId)
                            .OrderByDescending(vi => vi.CreatedAt)
                            .Select(vi => vi.WeddingDate)
                            .FirstOrDefault() != null
                            ? _context.VendorInquiries
                                .Where(vi => vi.UserId == u.UserId)
                                .OrderByDescending(vi => vi.CreatedAt)
                                .Select(vi => vi.WeddingDate!.Value.ToString("MMM dd, yyyy"))
                                .FirstOrDefault()
                            : "N/A",
                        inquiriesSent = _context.VendorInquiries.Count(vi => vi.UserId == u.UserId),
                        inquiriesCount = _context.VendorInquiries.Count(vi => vi.UserId == u.UserId)
                    })
                    .ToListAsync();

                var allCustomers = customersFromTable.Concat(customerUsers).ToList();
                return Ok(allCustomers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve customers.");
                return StatusCode(500, new { message = "An error occurred while fetching customers." });
            }
        }

        // GET: /api/customers/{id} or /api/customer-management/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCustomerDetails(int id)
        {
            try
            {
                var customer = await _context.Customers
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CustomerId == id);

                int userId = customer?.UserId ?? id;

                var user = customer?.User ?? await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);

                if (customer == null && user == null)
                {
                    return NotFound(new { message = $"Customer with ID {id} was not found." });
                }

                var inquiries = await _context.VendorInquiries
                    .Include(vi => vi.Vendor)
                    .Include(vi => vi.VendorService)
                    .Where(vi => (customer != null && vi.CustomerId == customer.CustomerId) || vi.UserId == userId)
                    .OrderByDescending(vi => vi.CreatedAt)
                    .Select(vi => new
                    {
                        inquiryId = vi.InquiryId,
                        vendorName = vi.Vendor != null ? vi.Vendor.BusinessName : "Unknown Vendor",
                        serviceName = vi.VendorService != null ? vi.VendorService.ServiceName : "Wedding Service",
                        eventDate = vi.WeddingDate != null ? vi.WeddingDate.Value.ToString("MMM dd, yyyy") : "N/A",
                        guestCount = vi.GuestCount,
                        budget = vi.Budget,
                        status = vi.Status ?? "Pending",
                        message = vi.Message,
                        createdAt = vi.CreatedAt.ToString("MMM dd, yyyy, hh:mm tt")
                    })
                    .ToListAsync();

                var weddingDate = inquiries.FirstOrDefault(i => i.eventDate != "N/A")?.eventDate ?? "N/A";

                string coupleName = customer != null
                    ? ((customer.FirstName ?? "") + " " + (customer.LastName ?? "")).Trim()
                    : user?.FullName ?? "Registered Couple";

                var result = new
                {
                    customerId = customer?.CustomerId ?? user!.UserId,
                    id = customer?.CustomerId ?? user!.UserId,
                    userId = userId,
                    coupleName = coupleName,
                    coupleNames = coupleName,
                    firstName = customer?.FirstName ?? user?.FullName ?? "",
                    lastName = customer?.LastName ?? "",
                    email = user != null ? user.Email : string.Empty,
                    phone = user != null ? (user.PhoneNumber ?? "N/A") : "N/A",
                    status = user != null && user.IsActive ? "Active" : "Inactive",
                    joinDate = (customer?.CreatedAt ?? user?.CreatedAt ?? DateTime.UtcNow).ToString("MMM dd, yyyy"),
                    weddingDate = weddingDate,
                    inquiriesSent = inquiries.Count,
                    inquiriesCount = inquiries.Count,
                    inquiries = inquiries
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve customer details for ID {Id}.", id);
                return StatusCode(500, new { message = "An error occurred while fetching customer details." });
            }
        }

        // DELETE: /api/customers/{id} or /api/customer-management/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                var customer = await _context.Customers
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CustomerId == id);

                var user = customer?.User ?? await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);

                if (customer == null && user == null)
                {
                    return NotFound(new { message = $"Customer with ID {id} was not found." });
                }

                int userId = customer?.UserId ?? user!.UserId;
                int customerId = customer?.CustomerId ?? 0;

                // 1. Remove related inquiries
                var inquiries = await _context.VendorInquiries
                    .Where(vi => (customerId > 0 && vi.CustomerId == customerId) || vi.UserId == userId)
                    .ToListAsync();
                if (inquiries.Any())
                {
                    _context.VendorInquiries.RemoveRange(inquiries);
                }

                // 2. Remove related favorites
                var favorites = await _context.CustomerFavorites
                    .Where(cf => (customerId > 0 && cf.CustomerId == customerId) || cf.UserId == userId)
                    .ToListAsync();
                if (favorites.Any())
                {
                    _context.CustomerFavorites.RemoveRange(favorites);
                }

                // 3. Remove customer record if present
                if (customer != null)
                {
                    _context.Customers.Remove(customer);
                }

                // 4. Remove user record
                if (user != null)
                {
                    _context.Users.Remove(user);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted customer ID {CustomerId} and User {UserId}.", id, userId);
                return Ok(new { message = "Customer deleted successfully.", customerId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete customer with ID {Id}.", id);
                return StatusCode(500, new { message = "An error occurred while deleting the customer." });
            }
        }
    }
}