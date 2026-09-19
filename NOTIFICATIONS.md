# System Notifications Integration Guide

This document outlines the notification architecture for the Oleena platform, the endpoints available to clients, and the exact integration calls required for each notification type across backend features.

---

## 1. Notification Architecture

- **Entity**: `Backend.Entities.Notification` (stores `NotificationId`, `UserId`, `Title`, `Message`, `Type`, `IsRead`, `CreatedAt`, `UpdatedAt`).
- **Database Indexes**: Indexed on `(UserId)` and composite `(UserId, IsRead)` for fast user notification lookups.
- **Service**: `INotificationService` / `NotificationService`
  - `GetNotificationsAsync(int userId)`: Fetches all notifications for the user ordered by newest first.
  - `MarkAsReadAsync(int userId, int notificationId)`: Marks a specific notification as read (scoped to caller's `UserId`).
  - `MarkAllAsReadAsync(int userId)`: Marks all unread notifications as read in one batch.
  - `DeleteNotificationAsync(int userId, int notificationId)`: Permanently dismisses/deletes a notification (scoped to caller's `UserId`).
  - `CreateAsync(int userId, string type, string title, string message)`: Creates and persists a new notification for any user.
- **Controller**: `Backend.Controllers.NotificationsController` at route `/api/notifications` (`[Authorize]`).
  - `GET /api/notifications` — Retrieve all notifications for the caller.
  - `PATCH /api/notifications/{id}/read` — Mark one notification as read (404 if not found or not owned by caller).
  - `PATCH /api/notifications/read-all` — Mark all unread notifications of caller as read.
  - `DELETE /api/notifications/{id}` — Delete a notification (404 if not found or not owned by caller).

---

## 2. Currently Wired Triggers (Phase 2)

### `ListingPublished`
- **Constant**: `NotificationTypes.ListingPublished`
- **Owner / Feature**: Vendor Content Management / Listing Wizard
- **Location**: `Backend.Services.VendorContentService` in `AddServiceAsync` and `UpdateServiceAsync`.
- **Trigger Condition**: When a listing is created directly with `Status == "Published"` or when an existing listing transitions from another status to `"Published"`.
- **Wiring Detail**: Injected directly into the same EF Core `SaveChangesAsync` transaction (not a separate call):
  ```csharp
  _context.Notifications.Add(new Notification
  {
      UserId = userId,
      Title = "Listing Published",
      Message = $"Your listing \"{service.ServiceName}\" has been published successfully.",
      Type = NotificationTypes.ListingPublished,
      IsRead = false
  });
  ```

### `SecurityChange`
- **Constant**: `NotificationTypes.SecurityChange`
- **Owner / Feature**: Account Security & Authentication
- **Location**: `Backend.Controllers.AdminSettingsController` in `SavePin` and `ChangePassword`.
- **Trigger Condition**: When an admin successfully changes their PIN or password.
- **Wiring Detail**: 
  ```csharp
  await _notificationService.CreateAsync(userId.Value, NotificationTypes.SecurityChange, "Security Settings Updated", "Your PIN was changed successfully.");
  ```

### `ProfileUpdated`
- **Constant**: `NotificationTypes.ProfileUpdated`
- **Owner / Feature**: Admin Profile Management
- **Location**: `Backend.Controllers.AdminSettingsController` in `UpdateProfile`.
- **Trigger Condition**: When an admin successfully updates their profile details.
- **Wiring Detail**: 
  ```csharp
  await _notificationService.CreateAsync(userId.Value, NotificationTypes.ProfileUpdated, "Profile Updated", "Your admin profile information has been successfully updated.");
  ```

### `AdminAccountCreated`
- **Constant**: `NotificationTypes.AdminAccountCreated`
- **Owner / Feature**: Admin Management
- **Location**: `Backend.Controllers.AdminManagementController` in `CreateAdmin`.
- **Trigger Condition**: When a new admin account is created.
- **Wiring Detail**: Broadcasted to all admins except the creator:
  ```csharp
  await _notificationService.CreateForAllAdminsAsync(NotificationTypes.AdminAccountCreated, "New Admin Account Created", $"A new administrator account ({result.FullName}) has been created.", currentUserId);
  ```

---

## 3. Pending Notification Triggers to Wire

For every unwired constant in `Backend.Constants.NotificationTypes`, the table below specifies the feature, owner, target method, and the exact one-line invocation to add once the respective feature/method is implemented.

| Constant | Feature / Module | Owner | Target File & Method | Exact Call |
| :--- | :--- | :--- | :--- | :--- |
| `ListingCreated` | Vendor Content / Listing Wizard | Praneeth | `VendorContentService.AddServiceAsync` (when saved as Draft) | `await _notificationService.CreateAsync(userId, NotificationTypes.ListingCreated, "Listing Created", $"Your listing \"{service.ServiceName}\" was created as a draft.");` |
| `ListingApproved` | Admin Listing Moderation | Praneeth / Admin | `AdminManagementService.ApproveListingAsync` (when admin approves a submitted listing) | `await _notificationService.CreateAsync(vendor.UserId, NotificationTypes.ListingApproved, "Listing Approved", $"Your listing \"{service.ServiceName}\" has been approved and published.");` |
| `ListingRejected` | Admin Listing Moderation | Praneeth / Admin | `AdminManagementService.RejectListingAsync` (when admin rejects a listing) | `await _notificationService.CreateAsync(vendor.UserId, NotificationTypes.ListingRejected, "Listing Rejected", $"Your listing \"{service.ServiceName}\" was rejected. Reason: {rejectionReason}");` |
| `ListingFlagged` | Content Moderation & Quality | Praneeth / Admin | `AdminManagementService.FlagListingAsync` (when listing is flagged for violation) | `await _notificationService.CreateAsync(vendor.UserId, NotificationTypes.ListingFlagged, "Listing Flagged", $"Your listing \"{service.ServiceName}\" has been flagged for review.");` |
| `ListingSubmitted` | Listing Review Queue | Praneeth | `VendorContentService.SubmitForReviewAsync` (when vendor submits listing for review) | `await _notificationService.CreateAsync(adminUserId, NotificationTypes.ListingSubmitted, "Listing Submitted for Review", $"Vendor \"{vendor.BusinessName}\" submitted listing \"{service.ServiceName}\" for review.");` |
| `CredentialVerified` | Vendor Credentials & Verification | Vinu | `VendorProfileService.VerifyDocumentAsync` (when admin verifies document) | `await _notificationService.CreateAsync(vendor.UserId, NotificationTypes.CredentialVerified, "Credential Verified", $"Your verification document \"{document.DocumentName}\" has been approved.");` |
| `CredentialRejected` | Vendor Credentials & Verification | Vinu | `VendorProfileService.RejectDocumentAsync` (when admin rejects document) | `await _notificationService.CreateAsync(vendor.UserId, NotificationTypes.CredentialRejected, "Credential Rejected", $"Your verification document \"{document.DocumentName}\" was rejected. Reason: {reason}");` |
| `CredentialExpired` | Background Credential Expiration Monitor | Vinu | `CredentialExpirationWorker.ExecuteAsync` (scheduled daily background task) | `await _notificationService.CreateAsync(vendor.UserId, NotificationTypes.CredentialExpired, "Credential Expired", $"Your document \"{document.DocumentName}\" has expired. Please upload an updated copy.");` |
| `CredentialSubmitted` | Vendor Credentials Upload Flow | Vinu | `VendorProfileService.UploadDocumentAsync` (when vendor uploads a new credential) | `await _notificationService.CreateAsync(adminUserId, NotificationTypes.CredentialSubmitted, "New Credential Submitted", $"Vendor \"{vendor.BusinessName}\" submitted \"{document.DocumentName}\" for verification.");` |
| `VendorRegistered` | Admin Vendor Approval Queue | Admin Team | `AdminManagementService` (when vendor completes signup and enters approval queue) | `await _notificationService.CreateAsync(adminUserId, NotificationTypes.VendorRegistered, "New Vendor Registered", $"A new vendor, {businessName}, has registered and is awaiting approval.");` |
| `ContentFlagged` | Admin Moderation Queue | Admin / Support Team | `ModerationService.ReportContentAsync` (when user/customer reports content) | `await _notificationService.CreateAsync(adminUserId, NotificationTypes.ContentFlagged, "Content Flagged for Review", $"Content #{contentId} has been flagged by a user and requires moderator review.");` |
| `DisputeFiled` | Customer Bookings & Dispute Resolution | Support Team | `DisputeService.CreateDisputeAsync` (when customer or vendor files dispute) | `await _notificationService.CreateAsync(targetUserId, NotificationTypes.DisputeFiled, "Dispute Filed", $"A dispute has been opened regarding booking #{bookingId}. Our support team is reviewing it.");` |
| `AiApprovalRequired` | AI Human-Approval Gate | AI Agent System | Pending AI Approval Gate Feature (Not yet built) | N/A - Tied to upcoming AI agent feature |

> [!IMPORTANT]
> **Important Distinction: `ContentFlagged` vs. `ListingFlagged`**
> These two notification types represent two different moments and recipients in the same flagging event:
> 1. **`ContentFlagged` (Admin-facing)**: Sent to `adminUserId` immediately when a customer or user reports a listing or review, alerting admins that an item needs review in the moderation queue.
> 2. **`ListingFlagged` (Vendor-facing)**: Sent to `vendor.UserId` after an admin investigates the reported item and officially flags the listing for policy violations or required revisions.

> [!NOTE]
> **`VendorRegistered` is Admin-facing**
> `NotificationTypes.VendorRegistered` is not a vendor welcome notification. It is an admin-facing alert sent to `adminUserId` when a vendor completes registration and enters the pending approval queue awaiting document and identity verification.

---

## 4. How Teammates Should Inject and Use INotificationService

To integrate notifications into your feature service:

1. In your service constructor, inject `INotificationService`:
   ```csharp
   private readonly INotificationService _notificationService;

   public MyFeatureService(INotificationService notificationService, ...)
   {
       _notificationService = notificationService;
   }
   ```
2. Call `CreateAsync` at the appropriate business logic trigger:
   ```csharp
   await _notificationService.CreateAsync(
       userId: targetUser.UserId,
       type: NotificationTypes.CredentialVerified,
       title: "Credential Verified",
       message: $"Your verification document '{document.DocumentName}' has been verified."
   );
   ```
3. If creating notifications within the exact same database transaction as an entity update (like listing publishing), you can alternatively add a `Notification` entity directly to `_context.Notifications` before `await _context.SaveChangesAsync()`.
