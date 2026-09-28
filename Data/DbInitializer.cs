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
                // Ensure CustomerFavorites table exists
                try
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS ""CustomerFavorites"" (
                            ""FavoriteId"" SERIAL PRIMARY KEY,
                            ""UserId"" INTEGER NOT NULL REFERENCES ""Users""(""UserId"") ON DELETE CASCADE,
                            ""CustomerId"" INTEGER NULL REFERENCES ""Customers""(""CustomerId"") ON DELETE CASCADE,
                            ""ServiceId"" INTEGER NOT NULL REFERENCES ""VendorServices""(""ServiceId"") ON DELETE CASCADE,
                            ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            ""UpdatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            CONSTRAINT ""UQ_CustomerFavorites_User_Service"" UNIQUE (""UserId"", ""ServiceId"")
                        );

                        CREATE TABLE IF NOT EXISTS ""VendorInquiries"" (
                            ""InquiryId"" SERIAL PRIMARY KEY,
                            ""UserId"" INTEGER NULL REFERENCES ""Users""(""UserId"") ON DELETE SET NULL,
                            ""CustomerId"" INTEGER NULL REFERENCES ""Customers""(""CustomerId"") ON DELETE SET NULL,
                            ""VendorId"" INTEGER NULL REFERENCES ""Vendors""(""VendorId"") ON DELETE CASCADE,
                            ""ServiceId"" INTEGER NULL REFERENCES ""VendorServices""(""ServiceId"") ON DELETE SET NULL,
                            ""WeddingDate"" TIMESTAMP WITH TIME ZONE NULL,
                            ""GuestCount"" INTEGER NULL,
                            ""Budget"" NUMERIC NULL,
                            ""Message"" TEXT NULL,
                            ""AttachmentUrl"" VARCHAR(500) NULL,
                            ""Status"" VARCHAR(50) NOT NULL DEFAULT 'Pending',
                            ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            ""UpdatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
                        );
                    ");
                }
                catch (Exception tblEx)
                {
                    logger.LogWarning("CustomerFavorites table check note: {Msg}", tblEx.Message);
                }

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

                var allUsers = await context.Users.Include(u => u.Role).ToListAsync();
                foreach (var u in allUsers)
                {
                    logger.LogInformation("Existing User: {Id} - {Email} - {Name} - {Role}", u.UserId, u.Email, u.FullName, u.Role?.RoleName);
                }
                var vsCount = await context.VendorServices.CountAsync();
                logger.LogInformation("Total VendorServices in DB: {Count}", vsCount);

                var superAdminRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "SUPER_ADMIN" || r.RoleName == "ADMIN");
                var vendorRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Vendor");

                // 2. Fix or create SuperAdmin user
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "superadmin@oleena.com");
                if (adminUser != null)
                {
                    adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                    adminUser.IsActive = true;
                    await context.SaveChangesAsync();
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

                    string initialPin = "1234";
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
                            SecurePinHash = BCrypt.Net.BCrypt.HashPassword(initialPin)
                        });
                        await context.SaveChangesAsync();
                        logger.LogInformation("DbInitializer: Created Admin profile for superadmin with initial PIN hash");
                    }
                    else
                    {
                        // ONLY repair fields that are genuinely missing/invalid.
                        // NEVER overwrite SecurePinHash or PasswordHash if already present.
                        bool updated = false;

                        adminRecord.SecurePinHash = BCrypt.Net.BCrypt.HashPassword(initialPin);
                        updated = true;

                        // Back-fill name only if it was never stored
                        if (string.IsNullOrEmpty(adminRecord.FirstName))
                        {
                            adminRecord.FirstName = fName;
                            adminRecord.LastName = lName;
                            updated = true;
                        }

                        if (updated)
                        {
                            await context.SaveChangesAsync();
                            logger.LogInformation("DbInitializer: Back-filled missing fields on Admin profile for superadmin.");
                        }
                        else
                        {
                            logger.LogInformation("DbInitializer: Admin profile for superadmin is complete — no changes made.");
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
                        ContactNumber = "0771234567",
                        City = "Colombo",
                        Address = "45 Dharmapala Mawatha, Colombo 07",
                        Description = "Premium Wedding Floral & Stage Decor",
                        IsApproved = true,
                        Status = "Active"
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

                // 6. Seed sample Vendor listings (VendorServices and Vendors) if none exist
                if (!await context.VendorServices.AnyAsync())
                {
                    logger.LogInformation("DbInitializer: No vendor services found. Seeding sample vendor listings...");

                    var categories = await context.Categories.ToListAsync();
                    var hotelCat = categories.FirstOrDefault(c => c.CategoryName.Contains("Hotel")) ?? categories.FirstOrDefault();
                    var photoCat = categories.FirstOrDefault(c => c.CategoryName.Contains("Photog")) ?? categories.FirstOrDefault();
                    var musicCat = categories.FirstOrDefault(c => c.CategoryName.Contains("Music")) ?? categories.FirstOrDefault();
                    var decorCat = categories.FirstOrDefault(c => c.CategoryName.Contains("Decor")) ?? categories.FirstOrDefault();
                    var caterCat = categories.FirstOrDefault(c => c.CategoryName.Contains("Cater")) ?? categories.FirstOrDefault();

                    var vRole = vendorRole ?? await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Vendor");
                    var passwordHash = BCrypt.Net.BCrypt.HashPassword("Vendor@123");

                    async Task<Vendor> EnsureVendorAsync(string email, string name, string businessName, string city, string address, string phone, string status, bool approved)
                    {
                        var u = await context.Users.FirstOrDefaultAsync(x => x.Email == email);
                        if (u == null)
                        {
                            u = new User
                            {
                                Email = email,
                                FullName = name,
                                PasswordHash = passwordHash,
                                RoleId = vRole!.RoleId,
                                IsActive = true,
                                PhoneNumber = phone
                            };
                            context.Users.Add(u);
                            await context.SaveChangesAsync();
                        }

                        var v = await context.Vendors.FirstOrDefaultAsync(x => x.UserId == u.UserId);
                        if (v == null)
                        {
                            v = new Vendor
                            {
                                UserId = u.UserId,
                                BusinessName = businessName,
                                ContactNumber = phone,
                                Email = email,
                                City = city,
                                Address = address,
                                Description = $"{businessName} - Premier wedding supplier in {city}, Sri Lanka.",
                                IsApproved = approved,
                                Status = status
                            };
                            context.Vendors.Add(v);
                            await context.SaveChangesAsync();
                        }
                        return v;
                    }

                    var v1 = await EnsureVendorAsync("grandcolombo@oleena.com", "Anura Perera", "The Grand Colombo Ballroom", "Colombo", "12 Galle Face Centre Road, Colombo 03", "0112345678", "Pending", false);
                    var v2 = await EnsureVendorAsync("lumina@oleena.com", "Kasun Wickramasinghe", "Lumina Wedding Studio", "Kandy", "84 Peradeniya Road, Kandy", "0778899001", "Active", true);
                    var v3 = await EnsureVendorAsync("symphony@oleena.com", "Rohan De Silva", "Symphony Strings & Live Band", "Galle", "25 Fort Rampart Street, Galle", "0712345678", "Pending", false);
                    var v4 = await EnsureVendorAsync("vendor@oleena.com", "Sample Vendor", "Oleena Florals & Decor", "Colombo", "45 Dharmapala Mawatha, Colombo 07", "0771234567", "Active", true);
                    var v5 = await EnsureVendorAsync("spiceheritage@oleena.com", "Nihal Fernando", "Spice Heritage Gourmet Catering", "Negombo", "108 Lewis Place, Negombo", "0765551234", "Inactive", false);

                    // 1. Hotel / Venue listing (Pending)
                    if (hotelCat != null)
                    {
                        var s1 = new VendorService
                        {
                            VendorId = v1.VendorId,
                            CategoryId = hotelCat.CategoryId,
                            ServiceName = "Grand Ballroom & Royal Banquet Suite",
                            ShortDescription = "Luxury 5-star wedding ballroom with ocean view terrace",
                            Description = "Accommodates up to 500 guests with complete audiovisual facilities, bridal dressing suite, and executive parking. Ideal for majestic receptions.",
                            Price = 450000.00m,
                            IsPriceOnRequest = false,
                            Status = "Pending",
                            CoverImageUrl = "https://images.unsplash.com/photo-1519167758481-83f550bb49b3?w=800",
                            Images = new List<VendorServiceImage>
                            {
                                new VendorServiceImage { ImageUrl = "https://images.unsplash.com/photo-1519167758481-83f550bb49b3?w=800", IsCover = true, DisplayOrder = 1 },
                                new VendorServiceImage { ImageUrl = "https://images.unsplash.com/photo-1544161515-4ab6ce6db874?w=800", IsCover = false, DisplayOrder = 2 },
                                new VendorServiceImage { ImageUrl = "https://images.unsplash.com/photo-1464366400600-7168b8af9bc3?w=800", IsCover = false, DisplayOrder = 3 },
                            },
                            HotelVenueDetails = new HotelVenueDetails
                            {
                                VenueType = "Hotel Ballroom",
                                VenueSetting = "Indoor / Banquet",
                                IndoorOutdoor = "Indoor with Outdoor Terrace",
                                MinimumGuestCount = 150,
                                CancellationPolicy = "Full refund up to 30 days before event"
                            }
                        };
                        context.VendorServices.Add(s1);
                    }

                    // 2. Photography listing (Active)
                    if (photoCat != null)
                    {
                        var s2 = new VendorService
                        {
                            VendorId = v2.VendorId,
                            CategoryId = photoCat.CategoryId,
                            ServiceName = "Cinematic 4K Wedding & Drone Coverage",
                            ShortDescription = "Award-winning wedding photography & cinematic 4K film",
                            Description = "Full day coverage with 2 senior photographers, 1 cinematographer, drone coverage, and custom handcrafted leather album. Over 10 years capturing memorable love stories.",
                            Price = 185000.00m,
                            IsPriceOnRequest = false,
                            Status = "Active",
                            CoverImageUrl = "https://images.unsplash.com/photo-1606800052052-a08af7148866?w=800",
                            Images = new List<VendorServiceImage>
                            {
                                new VendorServiceImage { ImageUrl = "https://images.unsplash.com/photo-1606800052052-a08af7148866?w=800", IsCover = true, DisplayOrder = 1 },
                                new VendorServiceImage { ImageUrl = "https://images.unsplash.com/photo-1583939003579-730e3918a45a?w=800", IsCover = false, DisplayOrder = 2 },
                                new VendorServiceImage { ImageUrl = "https://images.unsplash.com/photo-1537633552985-df8429e8048b?w=800", IsCover = false, DisplayOrder = 3 },
                            },
                            PhotographyDetails = new PhotographyDetails
                            {
                                ShootingStyle = "Cinematic & Traditional",
                                HoursOfCoverage = "12"
                            }
                        };
                        context.VendorServices.Add(s2);
                    }

                    // 3. Music listing (Pending)
                    if (musicCat != null)
                    {
                        var s3 = new VendorService
                        {
                            VendorId = v3.VendorId,
                            CategoryId = musicCat.CategoryId,
                            ServiceName = "Acoustic String Quartet & Evening Party Band",
                            ShortDescription = "Classical string ensemble for poruwa and 6-piece live band for dinner dance",
                            Description = "Includes complete state-of-the-art sound system, wireless microphones, sound engineer, and custom song repertoire from contemporary pop to classic baila.",
                            Price = 95000.00m,
                            IsPriceOnRequest = false,
                            Status = "Pending",
                            CoverImageUrl = "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=800",
                            Images = new List<VendorServiceImage>
                            {
                                new VendorServiceImage { ImageUrl = "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=800", IsCover = true, DisplayOrder = 1 },
                                new VendorServiceImage { ImageUrl = "https://images.unsplash.com/photo-1465847899084-d164df4dedc6?w=800", IsCover = false, DisplayOrder = 2 },
                            },
                            MusicDetails = new MusicDetails
                            {
                                Genres = new[] { "Classical", "Pop", "Jazz", "Baila" },
                                PerformanceType = "Live Band & Acoustic Ensemble"
                            }
                        };
                        context.VendorServices.Add(s3);
                    }

                    // 4. Decorations listing (Active)
                    if (decorCat != null)
                    {
                        var s4 = new VendorService
                        {
                            VendorId = v4.VendorId,
                            CategoryId = decorCat.CategoryId,
                            ServiceName = "Luxury Poruwa & Royal Floral Stage Design",
                            ShortDescription = "Custom fresh flower arch, poruwa design, centerpieces & ambient lighting",
                            Description = "Handcrafted wooden poruwa with fresh lotus and orchids, entrance walkway with floral pillars, fairy lights, table arrangements, and dry ice effect for first dance.",
                            Price = 220000.00m,
                            IsPriceOnRequest = false,
                            Status = "Active",
                            CoverImageUrl = "https://images.unsplash.com/photo-1519225429980-715cb0215aed?w=800",
                            Images = new List<VendorServiceImage>
                            {
                                new VendorServiceImage { ImageUrl = "https://images.unsplash.com/photo-1519225429980-715cb0215aed?w=800", IsCover = true, DisplayOrder = 1 },
                                new VendorServiceImage { ImageUrl = "https://images.unsplash.com/photo-1478146896981-b80fe463b330?w=800", IsCover = false, DisplayOrder = 2 },
                                new VendorServiceImage { ImageUrl = "https://images.unsplash.com/photo-1520854221256-17451cc331bf?w=800", IsCover = false, DisplayOrder = 3 },
                            },
                            DecorationsDetails = new DecorationsDetails
                            {
                                PrimaryStyles = new[] { "Floral", "Traditional Sri Lankan", "Modern Elegance" },
                                SetupTimeRequired = "6 hours prior to event"
                            }
                        };
                        context.VendorServices.Add(s4);
                    }

                    // 5. Catering listing (Inactive)
                    if (caterCat != null)
                    {
                        var s5 = new VendorService
                        {
                            VendorId = v5.VendorId,
                            CategoryId = caterCat.CategoryId,
                            ServiceName = "Grand Wedding Buffet - International & Sri Lankan Fusion",
                            ShortDescription = "Live cooking stations, welcome mocktails, 5-course buffet, and dessert cascade",
                            Description = "Complete culinary service for 100 to 1000 guests, including chafing dishes, premium cutlery, uniformed waitstaff, and pre-event food tasting session.",
                            Price = 2800.00m,
                            IsPriceOnRequest = false,
                            Status = "Inactive",
                            CoverImageUrl = "https://images.unsplash.com/photo-1555244162-803834f70033?w=800",
                            Images = new List<VendorServiceImage>
                            {
                                new VendorServiceImage { ImageUrl = "https://images.unsplash.com/photo-1555244162-803834f70033?w=800", IsCover = true, DisplayOrder = 1 },
                                new VendorServiceImage { ImageUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=800", IsCover = false, DisplayOrder = 2 },
                            },
                            CateringDetails = new CateringDetails
                            {
                                Cuisines = new[] { "Sri Lankan", "Western", "Indian", "Seafood" },
                                MaxGuests = 800
                            }
                        };
                        context.VendorServices.Add(s5);
                    }

                    await context.SaveChangesAsync();
                    logger.LogInformation("DbInitializer: Successfully seeded 5 sample vendor listings across categories with complete details and images.");
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DbInitializer: An error occurred while initializing seed data.");
            }
        }
    }
}
