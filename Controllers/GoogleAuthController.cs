using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GoogleAuthController : ControllerBase
    {
        private readonly IGoogleTokenVerifier _tokenVerifier;
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;
        private readonly ILogger<GoogleAuthController> _logger;

        public GoogleAuthController(
            IGoogleTokenVerifier tokenVerifier,
            IAuthService authService,
            AppDbContext context,
            ILogger<GoogleAuthController> logger)
        {
            _tokenVerifier = tokenVerifier;
            _authService = authService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/GoogleAuth/sign-in
        /// Verifies a Google ID token and either authenticates an existing vendor
        /// or signals that registration is required.
        /// </summary>
        [HttpPost("sign-in")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(GoogleSignInResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> SignIn([FromBody] GoogleSignInRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // ── 1. Verify the Google ID token ──
            GoogleTokenResult googleResult;
            try
            {
                googleResult = await _tokenVerifier.VerifyAsync(request.IdToken);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Invalid Token");
            }
            catch (HttpRequestException)
            {
                return Problem(
                    detail: "Unable to verify your Google account at this time. Please try again later.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Service Unavailable");
            }
            catch (InvalidOperationException ex)
            {
                // Google:ClientId not configured
                _logger.LogError("Google sign-in failed: {Message}", ex.Message);
                return Problem(
                    detail: "Google sign-in is temporarily unavailable.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Service Unavailable");
            }

            // ── 2. Look up by external login (Provider=Google, ProviderSubject=sub) ──
            var externalLogin = await _context.UserExternalLogins
                .Include(uel => uel.User)
                    .ThenInclude(u => u!.Role)
                .FirstOrDefaultAsync(uel => uel.Provider == "Google" && uel.ProviderSubject == googleResult.Sub);

            if (externalLogin != null)
            {
                var linkedUser = externalLogin.User!;

                if (!linkedUser.IsActive)
                {
                    return Problem(
                        detail: "This account is not allowed to use Google sign-in.",
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Access Denied");
                }

                return Ok(new GoogleSignInResponse
                {
                    Status = "AUTHENTICATED",
                    Auth = _authService.BuildVendorLoginResponse(linkedUser)
                });
            }

            // ── 3. Look up by email (case-insensitive) ──
            var emailLower = googleResult.Email.ToLower();
            var existingUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);

            if (existingUser != null)
            {
                // Must be active Vendor — Admin/SuperAdmin must NEVER get a token from here
                var roleName = existingUser.Role?.RoleName ?? "";
                if (!roleName.Equals("Vendor", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Google sign-in denied for UserId {UserId}: role is {Role}, not Vendor",
                        existingUser.UserId, roleName);
                    return Problem(
                        detail: "This account is not allowed to use Google sign-in.",
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Access Denied");
                }

                if (!existingUser.IsActive)
                {
                    return Problem(
                        detail: "This account is not allowed to use Google sign-in.",
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Access Denied");
                }

                // Link the Google sub to this existing Vendor user
                _context.UserExternalLogins.Add(new UserExternalLogin
                {
                    UserId = existingUser.UserId,
                    Provider = "Google",
                    ProviderSubject = googleResult.Sub
                });
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Linked Google sub to existing Vendor UserId={UserId}", existingUser.UserId);

                return Ok(new GoogleSignInResponse
                {
                    Status = "AUTHENTICATED",
                    Auth = _authService.BuildVendorLoginResponse(existingUser)
                });
            }

            // ── 5. No user exists → tell frontend to show registration ──
            return Ok(new GoogleSignInResponse
            {
                Status = "REGISTRATION_REQUIRED",
                Prefill = new GooglePrefill
                {
                    Email = googleResult.Email,
                    FullName = googleResult.FullName
                }
            });
        }
    }
}
