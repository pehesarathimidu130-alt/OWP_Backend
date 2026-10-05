using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    /// <summary>
    /// AnalyticsController — supplies live aggregation data for the three-tab
    /// System Analytics dashboard on the frontend.
    ///
    /// Route prefix: /api/analytics
    ///
    /// Auth: Admin + SuperAdmin roles required in production.
    /// During local development the [AllowAnonymous] override on each action
    /// lets the frontend dev server reach the API without a token while the
    /// class-level [Authorize] keeps Swagger documented correctly.
    /// Remove the action-level [AllowAnonymous] attributes before deploying.
    /// </summary>
    [ApiController]
    [Route("api/analytics")]
    [Authorize(Roles = "Admin,SuperAdmin,ADMIN,SUPER_ADMIN,admin,superadmin")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(
            IAnalyticsService analyticsService,
            ILogger<AnalyticsController> logger)
        {
            _analyticsService = analyticsService;
            _logger           = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/analytics/vendors
        // Vendor tab: funnel, category breakdown, monthly trend, ban reasons.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("vendors")]
        [AllowAnonymous] // ← remove before production deploy
        [ProducesResponseType(typeof(AnalyticsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetVendorAnalytics([FromQuery] int months = 6)
        {
            if (months < 1 || months > 24)
                return BadRequest(new { message = "months must be between 1 and 24." });
            try
            {
                var result = await _analyticsService.GetVendorAnalyticsAsync(months);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET /api/analytics/vendors failed");
                return StatusCode(500, new { message = "Failed to retrieve vendor analytics." });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/analytics/customers
        // Customer tab: total, active/inactive, monthly registration trend.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("customers")]
        [AllowAnonymous] // ← remove before production deploy
        [ProducesResponseType(typeof(AnalyticsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCustomerAnalytics([FromQuery] int months = 6)
        {
            if (months < 1 || months > 24)
                return BadRequest(new { message = "months must be between 1 and 24." });
            try
            {
                var result = await _analyticsService.GetCustomerAnalyticsAsync(months);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET /api/analytics/customers failed");
                return StatusCode(500, new { message = "Failed to retrieve customer analytics." });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/analytics/admins
        // Admin tab: total admins, super-admin count, active count, trend.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("admins")]
        [AllowAnonymous] // ← remove before production deploy
        [ProducesResponseType(typeof(AnalyticsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAdminAnalytics([FromQuery] int months = 6)
        {
            if (months < 1 || months > 24)
                return BadRequest(new { message = "months must be between 1 and 24." });
            try
            {
                var result = await _analyticsService.GetAdminAnalyticsAsync(months);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET /api/analytics/admins failed");
                return StatusCode(500, new { message = "Failed to retrieve admin analytics." });
            }
        }
    }
}
