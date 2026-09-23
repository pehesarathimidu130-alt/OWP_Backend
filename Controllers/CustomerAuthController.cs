using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/auth/customer")]
    public class CustomerAuthController : ControllerBase
    {
        private readonly ICustomerAuthService _customerAuthService;
        private readonly ILogger<CustomerAuthController> _logger;

        public CustomerAuthController(ICustomerAuthService customerAuthService, ILogger<CustomerAuthController> logger)
        {
            _customerAuthService = customerAuthService;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/auth/customer/register
        /// Registers a new Customer.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CustomerAuthResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] CustomerRegisterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var response = await _customerAuthService.RegisterAsync(request);
                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Registration validation error: {Message}", ex.Message);
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflict"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during customer registration");
                return Problem(
                    detail: "An unexpected error occurred during registration.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }

        /// <summary>
        /// POST /api/auth/customer/login
        /// Authenticates a Customer.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CustomerAuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] CustomerLoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var response = await _customerAuthService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Login denied: {Message}", ex.Message);
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication Failed"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during customer login");
                return Problem(
                    detail: "An unexpected error occurred during login.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }
    }
}
