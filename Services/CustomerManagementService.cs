using Backend.DTOs;
using Microsoft.EntityFrameworkCore;
using Backend.Data;

namespace Backend.Services
{
    public class CustomerManagementService : ICustomerManagementService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CustomerManagementService> _logger;

        public CustomerManagementService(AppDbContext context, ILogger<CustomerManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CustomerManagementResponseDto>> GetAllCustomersAsync()
        {
            var customers = await _context.Customers
                .Include(c => c.User)
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return customers.Select(c => new CustomerManagementResponseDto
            {
                Id = c.CustomerId,
                CoupleNames = $"{c.FirstName} {c.LastName}",
                Email = c.User?.Email ?? "N/A",
                Phone = c.User?.PhoneNumber ?? "N/A",
                WeddingDate = "N/A", // Not stored in DB currently
                Location = "N/A", // Not stored in DB currently
                InquiriesCount = 0, // Mock for now until Inquiries feature exists
                Status = (c.User?.IsActive ?? true) ? "Active" : "Inactive",
                JoinDate = c.CreatedAt.ToString("MMM dd, yyyy"),
                AvatarInitials = GetInitials(c.FirstName, c.LastName)
            }).ToList();
        }

        public async Task<bool> ToggleCustomerStatusAsync(int id, string newStatus)
        {
            var customer = await _context.Customers.Include(c => c.User).FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null || customer.User == null) return false;

            customer.User.IsActive = (newStatus == "Active");
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            var customer = await _context.Customers.Include(c => c.User).FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null) return false;

            if (customer.User != null)
            {
                _context.Users.Remove(customer.User);
            }
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return true;
        }

        private string GetInitials(string first, string last)
        {
            var f = string.IsNullOrWhiteSpace(first) ? "" : first[0].ToString().ToUpper();
            var l = string.IsNullOrWhiteSpace(last) ? "" : last[0].ToString().ToUpper();
            return f + l;
        }
    }
}
