using Backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // =========================================================================
        // DEVELOPER GUIDE: HOW TO ADD A NEW TABLE
        // 1. Create your entity class inside the 'Entities/' folder (inheriting BaseAuditableEntity).
        // 2. Register it below as a DbSet using this exact template:
        //    public DbSet<YourEntityName> YourEntityNames { get; set; } = null!;
        // 3. Open your terminal in the backend directory and run:
        //    dotnet ef migrations add Add<YourEntityName>Table
        //    dotnet ef database update
        // =========================================================================

        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Admin> Admins { get; set; } = null!;
        public DbSet<Vendor> Vendors { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<VendorService> VendorServices { get; set; } = null!;
        public DbSet<VendorPerformance> VendorPerformances { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<VendorGalleryImage> VendorGalleryImages { get; set; } = null!;
        public DbSet<VendorDocument> VendorDocuments { get; set; } = null!;
        
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<VenueSpace> VenueSpaces { get; set; } = null!;
        public DbSet<CateringDetails> CateringDetails { get; set; } = null!;
        public DbSet<DecorationsDetails> DecorationsDetails { get; set; } = null!;
        public DbSet<HotelVenueDetails> HotelVenueDetails { get; set; } = null!;
        public DbSet<MusicDetails> MusicDetails { get; set; } = null!;
        public DbSet<PhotographyDetails> PhotographyDetails { get; set; } = null!;
        public DbSet<VendorServiceImage> VendorServiceImages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Unique index on Users.Email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // 1:1 relationship between User and Admin
            modelBuilder.Entity<Admin>()
                .HasKey(a => a.AdminId);

            modelBuilder.Entity<Admin>()
                .HasOne(a => a.User)
                .WithOne(u => u.Admin)
                .HasForeignKey<Admin>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==========================================================
            // Vendor Services & Details Relationships
            // ==========================================================
            
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, CategoryName = "Hotel / Venue", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { CategoryId = 2, CategoryName = "Photography", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { CategoryId = 3, CategoryName = "Music", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { CategoryId = 4, CategoryName = "Decorations", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { CategoryId = 5, CategoryName = "Catering", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );

            modelBuilder.Entity<VendorService>()
                .Property(vs => vs.Status)
                .HasDefaultValue("Draft");

            modelBuilder.Entity<VendorService>()
                .HasOne(vs => vs.Category)
                .WithMany()
                .HasForeignKey(vs => vs.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VendorService>()
                .HasMany(vs => vs.VenueSpaces)
                .WithOne(v => v.VendorService)
                .HasForeignKey(v => v.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VendorService>()
                .HasMany(vs => vs.Images)
                .WithOne(vsi => vsi.Service)
                .HasForeignKey(vsi => vsi.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VendorService>()
                .HasOne(vs => vs.CateringDetails)
                .WithOne(d => d.VendorService)
                .HasForeignKey<CateringDetails>(d => d.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VendorService>()
                .HasOne(vs => vs.DecorationsDetails)
                .WithOne(d => d.VendorService)
                .HasForeignKey<DecorationsDetails>(d => d.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VendorService>()
                .HasOne(vs => vs.HotelVenueDetails)
                .WithOne(d => d.VendorService)
                .HasForeignKey<HotelVenueDetails>(d => d.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VendorService>()
                .HasOne(vs => vs.MusicDetails)
                .WithOne(d => d.VendorService)
                .HasForeignKey<MusicDetails>(d => d.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VendorService>()
                .HasOne(vs => vs.PhotographyDetails)
                .WithOne(d => d.VendorService)
                .HasForeignKey<PhotographyDetails>(d => d.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public override int SaveChanges()
        {
            ApplyAuditInformation();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInformation();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditInformation()
        {
            var entries = ChangeTracker.Entries<BaseAuditableEntity>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
            foreach (var entry in entries)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}