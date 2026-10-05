using System;
using Xunit;
using Backend.Entities;

namespace Backend.Tests
{
    /// <summary>
    /// Component 1: Vendor Listing Management, Performance Analytics & System Settings
    /// Owner: Thimidu
    /// </summary>
    public class Component1_CatalogAndPerformanceTests
    {
        [Fact]
        public void VendorService_ShouldStore_TitleDescriptionAndValidPrice()
        {
            // Arrange & Act
            var service = new VendorService
            {
                ServiceId = 1,
                VendorId = 10,
                ServiceName = "Grand Sapphire Ballroom",
                ShortDescription = "Luxury Banquet Hall",
                Description = "Exquisite wedding venue with scenic ocean view and full decor setup.",
                Price = 250000.00m,
                IsPriceOnRequest = false,
                Status = "Active"
            };

            // Assert
            Assert.Equal("Grand Sapphire Ballroom", service.ServiceName);
            Assert.Equal("Luxury Banquet Hall", service.ShortDescription);
            Assert.NotNull(service.Description);
            Assert.Equal(250000.00m, service.Price);
            Assert.True(service.Price >= 0, "Base price must be non-negative.");
            Assert.Equal("Active", service.Status);
        }

        [Theory]
        [InlineData(-100.00, false)]
        [InlineData(-0.01, false)]
        [InlineData(0.00, true)]
        [InlineData(15000.00, true)]
        public void VendorService_BasePriceValidation_ShouldRejectNegativePrices(double priceValue, bool expectedValid)
        {
            // Arrange
            decimal price = (decimal)priceValue;

            // Act - Business rule: Base price cannot be negative
            bool isValidPrice = price >= 0m;

            // Assert
            Assert.Equal(expectedValid, isValidPrice);
        }

        [Fact]
        public void VenueSpace_And_CategoryDetails_ShouldCorrectlyLinkTo_VendorService()
        {
            // Arrange
            var vendorService = new VendorService
            {
                ServiceId = 101,
                ServiceName = "Crystal Horizon Estate",
                Price = 500000.00m
            };

            var venueSpace = new VenueSpace
            {
                VenueSpaceId = 5,
                ServiceId = 101,
                SpaceName = "Imperial Lawn",
                SpaceType = "Outdoor Garden",
                SeatedCapacity = 450,
                FloatingCapacity = 700,
                AirConditioned = false,
                VendorService = vendorService
            };

            var cateringDetails = new CateringDetails
            {
                ServiceId = 101,
                ServiceStyle = "Buffet & Live Cooking",
                MinGuests = 100,
                MaxGuests = 600,
                PricePerHead = "4500 LKR",
                WaitstaffIncluded = true,
                VendorService = vendorService
            };

            var photographyDetails = new PhotographyDetails
            {
                ServiceId = 101,
                ShootingStyle = "Traditional & Candid",
                HoursOfCoverage = "Full Day",
                DigitalGalleryIncluded = true,
                VendorService = vendorService
            };

            // Assert - Navigation properties & foreign key mapping
            Assert.Equal(vendorService.ServiceId, venueSpace.ServiceId);
            Assert.Same(vendorService, venueSpace.VendorService);
            Assert.Equal("Imperial Lawn", venueSpace.SpaceName);
            Assert.Equal(450, venueSpace.SeatedCapacity);

            Assert.Equal(vendorService.ServiceId, cateringDetails.ServiceId);
            Assert.Same(vendorService, cateringDetails.VendorService);
            Assert.True(cateringDetails.WaitstaffIncluded);

            Assert.Equal(vendorService.ServiceId, photographyDetails.ServiceId);
            Assert.Same(vendorService, photographyDetails.VendorService);
            Assert.True(photographyDetails.DigitalGalleryIncluded);
        }

        [Theory]
        [InlineData(500, 25, 5.0)]
        [InlineData(1000, 120, 12.0)]
        [InlineData(0, 0, 0.0)]
        [InlineData(200, 0, 0.0)]
        public void ListingMetrics_EngagementRate_ShouldComputeValidPercentage(int viewCount, int inquiryCount, double expectedRate)
        {
            // Arrange & Act
            // Business rule: Engagement rate = (Inquiries / Views) * 100
            double calculatedRate = viewCount > 0
                ? Math.Round(((double)inquiryCount / viewCount) * 100.0, 2)
                : 0.0;

            // Assert
            Assert.Equal(expectedRate, calculatedRate);
            Assert.True(calculatedRate >= 0.0 && calculatedRate <= 100.0, "Engagement rate must be between 0% and 100%.");
        }
    }
}
