using System.Security.Claims;
using System.Threading.Tasks;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/admin/activity-log")]
    [Route("api/admin/activity-logs")]
    [Authorize(Roles = "SuperAdmin,SUPER_ADMIN,Admin,ADMIN")]
    public class AdminActivityLogController : ControllerBase
    {
        private readonly IActivityLogService _activityLogService;

        public AdminActivityLogController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }

        /// <summary>
        /// GET /api/admin/activity-log
        /// Returns full unfiltered audit logs for SuperAdmin role.
        /// For Admin role, filters server-side to ActingAdminId = caller's own ID
        /// and ActionType in vendor, customer, or own login/logout/credential-change events.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetActivityLogs()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Caller identity could not be verified." });
            }

            var roleClaim = User.FindFirstValue(ClaimTypes.Role) ?? "Admin";
            var logs = await _activityLogService.GetLogsAsync(userId, roleClaim);
            return Ok(logs);
        }
    }
}
