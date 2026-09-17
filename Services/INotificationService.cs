using Backend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Services
{
    public interface INotificationService
    {
        Task<List<NotificationResponseDto>> GetNotificationsAsync(int userId);
        Task<bool> MarkAsReadAsync(int userId, int notificationId);
        Task MarkAllAsReadAsync(int userId);
        Task<bool> DeleteNotificationAsync(int userId, int notificationId);
        Task<NotificationResponseDto> CreateAsync(int userId, string type, string title, string message);
    }
}
