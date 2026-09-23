using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    /// <summary>
    /// ReportAnalyticsController — Read-only aggregation endpoints for the
    /// Directory Report Analytics admin page.
    /// Route prefix: /api/ReportAnalytics
    /// All endpoints require Admin or SuperAdmin role.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin,ADMIN,SUPER_ADMIN,admin,superadmin")]
    public class ReportAnalyticsController : ControllerBase
    {
        private readonly IReportAnalyticsService _analyticsService;
        private readonly ILogger<ReportAnalyticsController> _logger;

        public ReportAnalyticsController(
            IReportAnalyticsService analyticsService,
            ILogger<ReportAnalyticsController> logger)
        {
            _analyticsService = analyticsService;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/ReportAnalytics/summary
        // Returns headline KPIs: total vendors, approval rate, avg days to
        // decision, top category, funnel counts, and category breakdown.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ReportAnalyticsSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var result = await _analyticsService.GetSummaryAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET /api/ReportAnalytics/summary failed");
                return StatusCode(500, new { message = "Failed to retrieve analytics summary." });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/ReportAnalytics/monthly-applications?months=6
        // Returns one entry per calendar month for the last N months.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("monthly-applications")]
        [ProducesResponseType(typeof(List<MonthlyApplicationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMonthlyApplications([FromQuery] int months = 6)
        {
            if (months < 1 || months > 24)
                return BadRequest(new { message = "months must be between 1 and 24." });

            try
            {
                var result = await _analyticsService.GetMonthlyApplicationsAsync(months);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET /api/ReportAnalytics/monthly-applications failed");
                return StatusCode(500, new { message = "Failed to retrieve monthly application data." });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/ReportAnalytics/ban-suspension-reasons
        // Returns normalised ban/suspension reason buckets, ordered by count.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("ban-suspension-reasons")]
        [ProducesResponseType(typeof(List<BanSuspensionReasonDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBanSuspensionReasons()
        {
            try
            {
                var result = await _analyticsService.GetBanSuspensionReasonsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET /api/ReportAnalytics/ban-suspension-reasons failed");
                return StatusCode(500, new { message = "Failed to retrieve ban/suspension reason data." });
            }
        }
    }
}
