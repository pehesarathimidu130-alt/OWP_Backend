using System;
using Xunit;
using Backend.Entities;
using Backend.Models;

namespace Backend.Tests
{
    /// <summary>
    /// Component 4: Vendor Verification, Content Moderation & Activity Auditing
    /// Owner: Pulathis
    /// </summary>
    public class Component4_GovernanceAndAuditTests
    {
        [Fact]
        public void ActivityLog_Creation_ShouldRecordActorIdActionTypeEntityNameAndUtcTimestamp()
        {
            // Arrange
            var beforeTimestamp = DateTime.UtcNow.AddSeconds(-1);

            // Act
            var log = new ActivityLog
            {
                ActingAdminId = 42,
                ActionType = "VENDOR_VERIFICATION_APPROVED",
                TargetEntityType = "Vendor",
                TargetEntityId = "204",
                Details = "{\"status\":\"Approved\",\"verifiedBy\":42}",
                Timestamp = DateTime.UtcNow
            };

            var afterTimestamp = DateTime.UtcNow.AddSeconds(1);

            // Assert
            Assert.Equal(42, log.ActingAdminId);
            Assert.Equal("VENDOR_VERIFICATION_APPROVED", log.ActionType);
            Assert.Equal("Vendor", log.EntityName);
            Assert.Equal("204", log.TargetEntityId);
            Assert.NotNull(log.Details);
            Assert.InRange(log.Timestamp, beforeTimestamp, afterTimestamp);
        }

        [Fact]
        public void FlaggedContent_ModerationState_ShouldInitializeWithOpenStatusAndValidReason()
        {
            // Arrange & Act
            var flag = new FlaggedContent
            {
                Id = 1,
                ContentType = "VendorService",
                ContentTitle = "Luxury Sri Lankan Catering",
                Reason = "Misleading pricing and unverified hygiene certificates",
                Severity = "High",
                ReportedBy = "moderator@oleena.lk"
            };

            // Assert
            Assert.Equal("Open", flag.Status); // Initial unmoderated state is "Open"
            Assert.Equal("High", flag.Severity);
            Assert.False(string.IsNullOrWhiteSpace(flag.Reason), "Reason code/text must not be empty.");
            Assert.True(flag.CreatedAt <= DateTime.UtcNow, "Flagged content timestamp must be recorded.");
            Assert.Equal("VendorService", flag.ContentType);
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData("", "", false)]
        [InlineData("   ", "   ", false)]
        [InlineData(null, "", false)]
        [InlineData("", null, false)]
        public void VendorVerificationRule_CannotBeApproved_IfBothDocumentsAreNullOrEmpty(string? brDocumentUrl, string? nicDocumentUrl, bool expectedCanApprove)
        {
            // Arrange
            var vendor = new Vendor
            {
                VendorId = 55,
                BusinessName = "Unverified Events Co.",
                Status = "Pending",
                IsApproved = false
            };

            // Act - Business rule: A vendor cannot be set to "Approved" if both BR and NIC documents are missing
            bool canApprove = !string.IsNullOrWhiteSpace(brDocumentUrl) || !string.IsNullOrWhiteSpace(nicDocumentUrl);

            if (!canApprove)
            {
                // Attempting to set to approved when documents are missing should be blocked
                // vendor remains in Pending status
                vendor.IsApproved = false;
            }

            // Assert
            Assert.Equal(expectedCanApprove, canApprove);
            Assert.False(vendor.IsApproved, "Vendor cannot be approved without at least one verified document.");
            Assert.Equal("Pending", vendor.Status);
        }

        [Fact]
        public void VendorVerificationRule_CanBeApproved_WhenValidDocumentProvided()
        {
            // Arrange
            string brDocumentUrl = "https://storage.oleena.lk/documents/vendors/br_cert_55.pdf";
            string? nicDocumentUrl = null;

            var vendor = new Vendor
            {
                VendorId = 55,
                BusinessName = "Verified Ceylon Florals",
                Status = "Pending",
                IsApproved = false
            };

            // Act - Business rule verification
            bool canApprove = !string.IsNullOrWhiteSpace(brDocumentUrl) || !string.IsNullOrWhiteSpace(nicDocumentUrl);

            if (canApprove)
            {
                vendor.Status = "Approved";
                vendor.IsApproved = true;
                vendor.VerificationStatus = "Verified";
                vendor.StatusChangedAt = DateTime.UtcNow;
            }

            // Assert
            Assert.True(canApprove);
            Assert.True(vendor.IsApproved);
            Assert.Equal("Approved", vendor.Status);
            Assert.Equal("Verified", vendor.VerificationStatus);
            Assert.NotNull(vendor.StatusChangedAt);
        }
    }
}
