using Backend.Data;
using Backend.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<NotificationResponseDto>> GetNotificationsAsync(int userId, int? page = null, int? pageSize = null)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt);

            IQueryable<Entities.Notification> pagedQuery = query;
            if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
            {
                pagedQuery = query.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
            }

            var notifications = await pagedQuery
                .Select(n => new NotificationResponseDto
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return notifications;
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(int userId, int notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null)
            {
                return false;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Count > 0)
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> DeleteNotificationAsync(int userId, int notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null)
            {
                return false;
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<NotificationResponseDto> CreateAsync(int userId, string type, string title, string message)
        {
            // If recipient is an admin, check whether they opted into this notification type
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId);
            if (admin != null)
            {
                bool allowed = true;
                if (type == Constants.NotificationTypes.VendorRegistered || type == Constants.NotificationTypes.ListingSubmitted)
                {
                    allowed = admin.NotifyNewVendorPending;
                }
                else if (type == Constants.NotificationTypes.ListingFlagged || type == Constants.NotificationTypes.ContentFlagged)
                {
                    allowed = admin.NotifyFlaggedContent;
                }
                else if (type == Constants.NotificationTypes.DisputeFiled)
                {
                    allowed = admin.NotifyCustomerComplaint;
                }
                else if (type == Constants.NotificationTypes.AiApprovalRequired)
                {
                    allowed = admin.NotifyAiWorkflowApproval;
                }

                if (!allowed)
                {
                    return new NotificationResponseDto
                    {
                        Title = title,
                        Message = message,
                        Type = type,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                }
            }

            var notification = new Backend.Entities.Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return new NotificationResponseDto
            {
                NotificationId = notification.NotificationId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }

        public async Task CreateForAllAdminsAsync(string type, string title, string message, int? excludeUserId = null)
        {
            var query = _context.Admins.AsQueryable();
            if (excludeUserId != null)
            {
                query = query.Where(a => a.UserId != excludeUserId);
            }

            // Respect admin notification preferences
            if (type == Constants.NotificationTypes.VendorRegistered || type == Constants.NotificationTypes.ListingSubmitted)
            {
                query = query.Where(a => a.NotifyNewVendorPending);
            }
            else if (type == Constants.NotificationTypes.ListingFlagged || type == Constants.NotificationTypes.ContentFlagged)
            {
                query = query.Where(a => a.NotifyFlaggedContent);
            }
            else if (type == Constants.NotificationTypes.DisputeFiled)
            {
                query = query.Where(a => a.NotifyCustomerComplaint);
            }
            else if (type == Constants.NotificationTypes.AiApprovalRequired)
            {
                query = query.Where(a => a.NotifyAiWorkflowApproval);
            }

            var adminUserIds = await query
                .Select(a => a.UserId)
                .ToListAsync();

            if (adminUserIds.Count == 0) return;

            var notifications = adminUserIds.Select(userId => new Backend.Entities.Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                IsRead = false
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
        }
    }
}
