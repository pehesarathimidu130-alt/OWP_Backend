using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VendorRegistrationController : ControllerBase
    {
        private readonly IVendorRegistrationService _registrationService;
        private readonly ILogger<VendorRegistrationController> _logger;

        public VendorRegistrationController(
            IVendorRegistrationService registrationService,
            ILogger<VendorRegistrationController> logger)
        {
            _registrationService = registrationService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/VendorRegistration/options
        /// Returns categories, districts, and business types for the registration form.
        /// </summary>
        [HttpGet("options")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RegistrationOptionsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOptions()
        {
            var options = await _registrationService.GetOptionsAsync();
            return Ok(options);
        }

        /// <summary>
        /// POST /api/VendorRegistration/register
        /// Registers a new vendor account and returns a login response (JWT + profile).
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] VendorRegistrationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var response = await _registrationService.RegisterAsync(request);
                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (ValidationException ex)
            {
                // Service-level validation errors → 400 with camelCase field keys
                foreach (var kvp in ex.Errors)
                {
                    foreach (var msg in kvp.Value)
                    {
                        ModelState.AddModelError(kvp.Key, msg);
                    }
                }
                return ValidationProblem(ModelState);
            }
            catch (DuplicateEmailException ex)
            {
                // 409 Conflict — email already registered
                _logger.LogWarning("Registration conflict: {Message}", ex.Message);
                return Conflict(new
                {
                    code = "EMAIL_ALREADY_REGISTERED",
                    field = "email",
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                // Google token invalid or expired → 401
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Invalid Token");
            }
            catch (HttpRequestException)
            {
                // Google unreachable → 503
                return Problem(
                    detail: "Unable to verify your Google account at this time. Please try again later.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Service Unavailable");
            }
            catch (InvalidOperationException ex)
            {
                // Server misconfiguration (Google:ClientId missing, Vendor role missing)
                _logger.LogError("Registration failed: {Message}", ex.Message);
                return Problem(
                    detail: "Registration is temporarily unavailable. Please try again later.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Service Unavailable");
            }
        }
    }
}
