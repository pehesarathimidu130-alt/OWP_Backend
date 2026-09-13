using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            try
            {
                // 1. Ensure essential roles exist
                var roles = await context.Roles.ToListAsync();
                if (!roles.Any())
                {
                    logger.LogInformation("DbInitializer: Seeding default roles...");
                    context.Roles.AddRange(
                        new Role { RoleName = "SUPER_ADMIN" },
                        new Role { RoleName = "ADMIN" },
                        new Role { RoleName = "Vendor" },
                        new Role { RoleName = "Customer" }
                    );
                    await context.SaveChangesAsync();
                }

                var superAdminRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "SUPER_ADMIN" || r.RoleName == "ADMIN");
                var vendorRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Vendor");

                // 2. Fix or create SuperAdmin user
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "superadmin@oleena.com");
                if (adminUser != null)
                {
                    // Check if PasswordHash is invalid / placeholder
                    if (string.IsNullOrEmpty(adminUser.PasswordHash) || !adminUser.PasswordHash.StartsWith("$2"))
                    {
                        adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                        adminUser.IsActive = true;
                        logger.LogInformation("DbInitializer: Fixed invalid PasswordHash for superadmin@oleena.com -> reset to Admin@123");
                        await context.SaveChangesAsync();
                    }
                }
                else if (superAdminRole != null)
                {
                    adminUser = new User
                    {
                        Email = "superadmin@oleena.com",
                        FullName = "System SuperAdmin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                        RoleId = superAdminRole.RoleId,
                        IsActive = true
                    };
                    context.Users.Add(adminUser);
                    await context.SaveChangesAsync();
                    logger.LogInformation("DbInitializer: Created user superadmin@oleena.com (password: Admin@123)");
                }

                // 3. Ensure Admin record with SecurePinHash exists for superadmin
                if (adminUser != null)
                {
                    var nameParts = (adminUser.FullName ?? "System SuperAdmin").Split(' ', 2);
                    var fName = nameParts[0];
                    var lName = nameParts.Length > 1 ? nameParts[1] : "SuperAdmin";

                    var adminRecord = await context.Admins.FirstOrDefaultAsync(a => a.UserId == adminUser.UserId);
                    if (adminRecord == null)
                    {
                        context.Admins.Add(new Admin
                        {
                            UserId = adminUser.UserId,
                            FirstName = fName,
                            LastName = lName,
                            Department = "Administration",
                            AccessLevel = "SuperAdmin",
                            SecurePinHash = BCrypt.Net.BCrypt.HashPassword("9999")
                        });
                        await context.SaveChangesAsync();
                        logger.LogInformation("DbInitializer: Created Admin profile for superadmin with PIN 9999 hash");
                    }
                    else
                    {
                        bool updated = false;
                        adminRecord.SecurePinHash = BCrypt.Net.BCrypt.HashPassword("9999");
                        updated = true;
                        if (string.IsNullOrEmpty(adminRecord.FirstName))
                        {
                            adminRecord.FirstName = fName;
                            adminRecord.LastName = lName;
                            updated = true;
                        }
                        if (updated)
                        {
                            await context.SaveChangesAsync();
                            logger.LogInformation("DbInitializer: Updated Admin profile for superadmin with SecurePinHash");
                        }
                    }
                }

                // 4. Seed sample Vendor user if not exists
                var vendorUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "vendor@oleena.com");
                if (vendorUser == null && vendorRole != null)
                {
                    vendorUser = new User
                    {
                        Email = "vendor@oleena.com",
                        FullName = "Sample Vendor",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Vendor@123"),
                        RoleId = vendorRole.RoleId,
                        IsActive = true
                    };
                    context.Users.Add(vendorUser);
                    await context.SaveChangesAsync();

                    context.Vendors.Add(new Vendor
                    {
                        UserId = vendorUser.UserId,
                        BusinessName = "Oleena Florals & Decor",
                        ContactNumber = "+94771234567",
                        Description = "Premium Wedding Floral & Stage Decor",
                        IsApproved = true,
                        Status = "Approved"
                    });
                    await context.SaveChangesAsync();
                    logger.LogInformation("DbInitializer: Created sample vendor user vendor@oleena.com (password: Vendor@123)");
                }

                // 5. Seed sample Customer user if not exists
                var customerRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
                var customerUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "customer@oleena.com");
                if (customerUser == null && customerRole != null)
                {
                    customerUser = new User
                    {
                        Email = "customer@oleena.com",
                        FullName = "Sample Customer",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                        RoleId = customerRole.RoleId,
                        IsActive = true
                    };
                    context.Users.Add(customerUser);
                    await context.SaveChangesAsync();

                    context.Customers.Add(new Customer
                    {
                        UserId = customerUser.UserId,
                        FirstName = "Sample",
                        LastName = "Customer"
                    });
                    await context.SaveChangesAsync();
                    logger.LogInformation("DbInitializer: Created sample customer user customer@oleena.com (password: Customer@123)");
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DbInitializer: An error occurred while initializing seed data.");
            }
        }
    }
}
