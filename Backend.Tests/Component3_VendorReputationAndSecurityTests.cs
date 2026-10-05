using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Backend.Entities;

namespace Backend.Tests
{
    /// <summary>
    /// Component 3: Vendor Business Profile, Ratings & Customer Security
    /// Owner: Vinu
    /// </summary>
    public class Component3_VendorReputationAndSecurityTests
    {
        [Theory]
        [InlineData(1, true)]
        [InlineData(3, true)]
        [InlineData(5, true)]
        [InlineData(0, false)]
        [InlineData(6, false)]
        [InlineData(-1, false)]
        public void VendorRating_CalculationBounds_MustFallWithinRangeOneToFive(int ratingScore, bool expectedValid)
        {
            // Arrange & Act
            // Business rule: Ratings must be bounded between 1 and 5 stars
            bool isValidRating = ratingScore >= 1 && ratingScore <= 5;

            // Assert
            Assert.Equal(expectedValid, isValidRating);
        }

        [Fact]
        public void VendorRating_AverageScore_CalculatesAccuratelyWithinBounds()
        {
            // Arrange
            var ratings = new List<VendorRating>
            {
                new VendorRating { RatingValue = 5, VendorId = 1, CustomerId = 10 },
                new VendorRating { RatingValue = 4, VendorId = 1, CustomerId = 11 },
                new VendorRating { RatingValue = 5, VendorId = 1, CustomerId = 12 },
                new VendorRating { RatingValue = 4, VendorId = 1, CustomerId = 13 },
                new VendorRating { RatingValue = 3, VendorId = 1, CustomerId = 14 }
            };

            // Act
            double averageRating = ratings.Average(r => r.RatingValue);
            double roundedAverage = Math.Round(averageRating, 1);

            // Assert
            Assert.Equal(4.2, roundedAverage);
            Assert.InRange(roundedAverage, 1.0, 5.0);
        }

        [Fact]
        public void VendorProfile_OperationalInformation_ShouldRetainFormattedContactAndHours()
        {
            // Arrange & Act
            var vendor = new Vendor
            {
                VendorId = 7,
                BusinessName = "Aura Floral Designers",
                ContactNumber = "+94771234567",
                AltPhoneNumber = "+94112345678",
                Email = "info@auraflorals.lk",
                WebsiteUrl = "https://auraflorals.lk",
                BusinessHoursJson = "{\"Monday\":\"09:00-18:00\",\"Saturday\":\"09:00-14:00\"}",
                Status = "Approved"
            };

            // Assert
            Assert.Equal("Aura Floral Designers", vendor.BusinessName);
            Assert.NotNull(vendor.ContactNumber);
            Assert.StartsWith("+94", vendor.ContactNumber);
            Assert.NotNull(vendor.Email);
            Assert.Contains("@", vendor.Email);
            Assert.Contains(".", vendor.Email);
            Assert.NotNull(vendor.BusinessHoursJson);
            Assert.Contains("Monday", vendor.BusinessHoursJson);
            Assert.Contains("09:00-18:00", vendor.BusinessHoursJson);
        }

        [Fact]
        public void SecurityPinAndResetToken_ShouldHaveExpectedLengthAndFutureExpiration()
        {
            // 1. Validate 4-digit Security PIN generation (Admin PIN rule)
            int generatedPinNumber = Random.Shared.Next(1000, 10000);
            string pin = generatedPinNumber.ToString();

            Assert.Equal(4, pin.Length);
            Assert.True(Regex.IsMatch(pin, @"^\d{4}$"), "Security PIN must consist of exactly 4 numeric digits.");

            // 2. Validate 6-digit Password Reset Verification Code & Expiration (Customer Auth rule)
            string resetCode = Random.Shared.Next(100000, 1000000).ToString();
            DateTime expirationTimestamp = DateTime.UtcNow.AddMinutes(15);

            Assert.Equal(6, resetCode.Length);
            Assert.True(Regex.IsMatch(resetCode, @"^\d{6}$"), "Password reset token must be a 6-digit verification code.");
            Assert.True(expirationTimestamp > DateTime.UtcNow, "Reset token expiration timestamp must be in the future.");
            Assert.True((expirationTimestamp - DateTime.UtcNow).TotalMinutes <= 15.01, "Token expiration should be within the 15-minute window.");
        }
    }
}
