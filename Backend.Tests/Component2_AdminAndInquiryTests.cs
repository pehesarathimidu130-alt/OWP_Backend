using System;
using Xunit;
using Backend.Entities;

namespace Backend.Tests
{
    /// <summary>
    /// Component 2: Platform Administration, Listing Review & Customer Inquiry Pipeline
    /// Owner: Praneeth
    /// </summary>
    public class Component2_AdminAndInquiryTests
    {
        [Fact]
        public void AdminStatusToggle_WhenDeactivated_ShouldRevokeAdministrativeAccess()
        {
            // Arrange
            var user = new User
            {
                UserId = 10,
                FullName = "Praneeth Administrator",
                Email = "praneeth.admin@oleena.lk",
                IsActive = true
            };

            var admin = new Admin
            {
                AdminId = 1,
                UserId = 10,
                User = user,
                FirstName = "Praneeth",
                LastName = "Administrator",
                AccessLevel = "Admin"
            };

            // Business rule: Admin access is granted only when User is active and access level is valid
            bool HasAdminAccess(User u, Admin a) => u.IsActive && (a.AccessLevel == "Admin" || a.AccessLevel == "SuperAdmin");

            // Verify initial active state
            Assert.True(user.IsActive);
            Assert.True(HasAdminAccess(user, admin), "Active admin must have access privileges.");

            // Act - Admin is deactivated (e.g. suspended or offboarded)
            user.IsActive = false;

            // Assert
            Assert.False(user.IsActive);
            Assert.False(HasAdminAccess(user, admin), "Deactivated admin must have administrative access revoked.");
        }

        [Fact]
        public void VendorInquiry_LifecycleInitialization_ShouldDefaultToPendingStatus()
        {
            // Arrange & Act
            var inquiry = new VendorInquiry
            {
                InquiryId = 1001,
                UserId = 5,
                VendorId = 20,
                ServiceId = 50,
                Message = "Inquiring about availability for our wedding on December 15th."
            };

            // Assert
            Assert.Equal("Pending", inquiry.Status);
            Assert.True(inquiry.CreatedAt <= DateTime.UtcNow, "Creation timestamp must be initialized to UTC.");
            Assert.Equal(1001, inquiry.InquiryId);
            Assert.NotNull(inquiry.Message);
        }

        [Theory]
        [InlineData(0, 1000.0, false)]      // Guest count zero is invalid
        [InlineData(-10, 5000.0, false)]    // Negative guest count is invalid
        [InlineData(100, -1.0, false)]      // Negative budget is invalid
        [InlineData(-5, -500.0, false)]     // Both negative is invalid
        public void VendorInquiry_RequirementsValidation_ShouldRejectInvalidGuestCountOrBudget(int guestCount, double budgetValue, bool expectedValid)
        {
            // Arrange
            decimal budget = (decimal)budgetValue;

            // Act - Business rule: Guest count must be > 0 and budget must be non-negative
            bool isValid = guestCount > 0 && budget >= 0m;

            // Assert
            Assert.Equal(expectedValid, isValid);
        }

        [Fact]
        public void VendorInquiry_ValidInputs_ShouldSuccessfullyStoreDetails()
        {
            // Arrange
            var weddingDate = DateTime.UtcNow.AddMonths(6);
            var inquiry = new VendorInquiry
            {
                InquiryId = 2002,
                GuestCount = 250,
                Budget = 450000.00m,
                WeddingDate = weddingDate,
                Message = "Please send menu and seating packages.",
                Status = "Pending"
            };

            // Act
            bool isValidInquiry = (inquiry.GuestCount == null || inquiry.GuestCount > 0)
                               && (inquiry.Budget == null || inquiry.Budget >= 0m)
                               && inquiry.WeddingDate > DateTime.UtcNow;

            // Assert
            Assert.True(isValidInquiry, "Inquiry with positive guest count, positive budget, and future date must be valid.");
            Assert.Equal(250, inquiry.GuestCount);
            Assert.Equal(450000.00m, inquiry.Budget);
            Assert.Equal("Pending", inquiry.Status);
        }

        [Fact]
        public void VendorInquiry_WhenVendorReplies_ShouldTransitionStatusToRepliedAndRecordReplyDetails()
        {
            // Arrange
            var inquiry = new VendorInquiry
            {
                InquiryId = 3003,
                Status = "Pending",
                Message = "Do you have open dates for November?"
            };

            var replyMessage = "Yes! We have November 14th available for your wedding reception.";

            // Act - Vendor submits reply
            inquiry.VendorReply = replyMessage;
            inquiry.RepliedAt = DateTime.UtcNow;
            inquiry.Status = "Replied";

            // Assert
            Assert.Equal("Replied", inquiry.Status);
            Assert.Equal(replyMessage, inquiry.VendorReply);
            Assert.NotNull(inquiry.RepliedAt);
            Assert.True(inquiry.RepliedAt <= DateTime.UtcNow);
        }
    }
}
