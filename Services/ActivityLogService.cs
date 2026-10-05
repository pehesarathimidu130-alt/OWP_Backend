using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ActivityLogService> _logger;

        public ActivityLogService(
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ActivityLogService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<ActivityLog> LogAsync(
            string actionType,
            string targetEntityType,
            string? targetEntityId,
            string? details,
            int? actingAdminId = null,
            bool saveChanges = true)
        {
            try
            {
                // Resolve acting admin ID if not explicitly passed
                int? resolvedAdminId = actingAdminId;
                if (!resolvedAdminId.HasValue)
                {
                    var user = _httpContextAccessor.HttpContext?.User;
                    var claim = user?.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (int.TryParse(claim, out var userId))
                    {
                        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId || a.AdminId == userId);
                        resolvedAdminId = admin?.AdminId;
                    }
                }

                var log = new ActivityLog
                {
                    ActingAdminId = resolvedAdminId,
                    ActionType = actionType,
                    TargetEntityType = targetEntityType,
                    TargetEntityId = targetEntityId,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

                _context.ActivityLogs.Add(log);

                if (saveChanges)
                {
                    await _context.SaveChangesAsync();
                }

                return log;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ActivityLogService: Failed to record audit log for action {ActionType} on {TargetType} #{TargetId}",
                    actionType, targetEntityType, targetEntityId);
                throw;
            }
        }

        public Task<ActivityLog> LogAsync(
            int? actingAdminId,
            string actionType,
            string targetEntityType,
            string? targetEntityId,
            string? details,
            bool saveChanges = true)
        {
            return LogAsync(actionType, targetEntityType, targetEntityId, details, actingAdminId, saveChanges);
        }

        public async Task<List<ActivityLogDto>> GetLogsAsync(int callerUserId, string callerRole)
        {
            var callerAdmin = await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId == callerUserId || a.AdminId == callerUserId);

            var isSuperAdmin = callerRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                               callerRole.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) ||
                               (callerAdmin?.AccessLevel.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ?? false);

            var query = _context.ActivityLogs
                .Include(l => l.ActingAdmin)
                    .ThenInclude(a => a != null ? a.User : null)
                .AsQueryable();

            if (!isSuperAdmin)
            {
                var callerAdminId = callerAdmin?.AdminId ?? 0;
                var allowedActionTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    // Vendor-related
                    ActivityLogTypes.VendorStatusChanged,
                    ActivityLogTypes.VendorFieldEdited,
                    // Customer-related
                    ActivityLogTypes.CustomerStatusToggled,
                    // Own login/logout/credential-change types
                    ActivityLogTypes.AdminLogin,
                    ActivityLogTypes.AdminLogout,
                    ActivityLogTypes.AdminPinChanged,
                    ActivityLogTypes.AdminPasswordChanged
                };

                query = query.Where(l => l.ActingAdminId == callerAdminId && allowedActionTypes.Contains(l.ActionType));
            }

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            return logs.Select(l =>
            {
                var actorName = l.ActingAdmin != null
                    ? $"{l.ActingAdmin.FirstName} {l.ActingAdmin.LastName}".Trim()
                    : "System";

                if (string.IsNullOrWhiteSpace(actorName) || actorName == " ")
                {
                    actorName = l.ActingAdmin?.User?.FullName ?? "System Admin";
                }

                var description = !string.IsNullOrWhiteSpace(l.Details)
                    ? l.Details
                    : FormatDefaultDescription(l.ActionType, l.TargetEntityType, l.TargetEntityId);

                return new ActivityLogDto
                {
                    Id = l.Id,
                    Timestamp = l.Timestamp.ToString("o"),
                    ActionType = l.ActionType,
                    TargetEntityType = l.TargetEntityType,
                    TargetEntityId = l.TargetEntityId,
                    Details = l.Details,
                    Description = description,
                    ActingAdminId = l.ActingAdminId,
                    ActorName = actorName,
                    ActorEmail = l.ActingAdmin?.User?.Email,
                    ActorRole = l.ActingAdmin?.AccessLevel ?? "Admin"
                };
            }).ToList();
        }

        private static string FormatDefaultDescription(string actionType, string targetType, string? targetId)
        {
            var targetSuffix = !string.IsNullOrEmpty(targetId) ? $" #{targetId}" : "";
            return actionType switch
            {
                ActivityLogTypes.AdminAccountCreated => $"Created administrator account{targetSuffix}",
                ActivityLogTypes.AdminAccountDeleted => $"Deleted administrator account{targetSuffix}",
                ActivityLogTypes.AdminPinChanged => $"Changed security PIN for administrator{targetSuffix}",
                ActivityLogTypes.AdminPasswordChanged => $"Changed account password{targetSuffix}",
                ActivityLogTypes.AdminRoleChanged => $"Updated role and access level for administrator{targetSuffix}",
                ActivityLogTypes.AdminLogin => "Administrator logged in to portal",
                ActivityLogTypes.AdminLogout => "Administrator logged out",
                ActivityLogTypes.VendorStatusChanged => $"Updated vendor status{targetSuffix}",
                ActivityLogTypes.VendorFieldEdited => $"Updated vendor profile details{targetSuffix}",
                ActivityLogTypes.CustomerStatusToggled => $"Toggled customer account status{targetSuffix}",
                _ => $"{actionType} on {targetType}{targetSuffix}"
            };
        }
    }
}
