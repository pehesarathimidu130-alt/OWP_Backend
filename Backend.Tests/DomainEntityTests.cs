using System;
using Xunit;
using Backend.Entities;

namespace Backend.Tests
{
    public class DomainEntityTests
    {
        [Fact]
        public void User_ShouldInitialize_WithCurrentTimestamp()
        {
            // Arrange & Act
            var user = new User 
            { 
                Email = "admin@oleena.com",
                Role = "SuperAdmin"
            };

            // Assert
            Assert.Equal("admin@oleena.com", user.Email);
            Assert.Equal("SuperAdmin", user.Role);
        }

        [Fact]
        public void Vendor_ShouldLinkToUser_AndSetBusinessName()
        {
            // Arrange & Act
            var vendor = new Vendor 
            { 
                BusinessName = "Aura Luxe Photography",
                Status = "Pending"
            };

            // Assert
            Assert.Equal("Aura Luxe Photography", vendor.BusinessName);
            Assert.Equal("Pending", vendor.Status);
        }

        [Fact]
        public void VendorInquiry_ShouldTrack_BudgetAndGuestCount()
        {
            // Arrange & Act
            var inquiry = new VendorInquiry 
            { 
                GuestCount = 150,
                Budget = 5000.00m,
                Status = "Pending"
            };

            // Assert
            Assert.Equal(150, inquiry.GuestCount);
            Assert.Equal(5000.00m, inquiry.Budget);
            Assert.Equal("Pending", inquiry.Status);
        }

        [Fact]
        public void ActivityLog_ShouldRecord_ActionTypeAndEntity()
        {
            // Arrange & Act
            var log = new ActivityLog 
            { 
                ActionType = "UPDATE",
                EntityName = "Vendor",
                Timestamp = DateTime.UtcNow
            };

            // Assert
            Assert.Equal("UPDATE", log.ActionType);
            Assert.Equal("Vendor", log.EntityName);
            Assert.True(log.Timestamp <= DateTime.UtcNow);
        }
    }
}
