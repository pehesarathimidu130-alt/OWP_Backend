using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.DTOs;
using Backend.Entities;

namespace Backend.Services
{
    public interface IActivityLogService
    {
        Task<ActivityLog> LogAsync(
            string actionType,
            string targetEntityType,
            string? targetEntityId,
            string? details,
            int? actingAdminId = null,
            bool saveChanges = true);

        Task<ActivityLog> LogAsync(
            int? actingAdminId,
            string actionType,
            string targetEntityType,
            string? targetEntityId,
            string? details,
            bool saveChanges = true);

        Task<List<ActivityLogDto>> GetLogsAsync(int callerUserId, string callerRole);
    }
}
