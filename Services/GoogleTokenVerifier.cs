using Google.Apis.Auth;

namespace Backend.Services
{
    public class GoogleTokenVerifier : IGoogleTokenVerifier
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleTokenVerifier> _logger;

        public GoogleTokenVerifier(IConfiguration configuration, ILogger<GoogleTokenVerifier> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<GoogleTokenResult> VerifyAsync(string idToken)
        {
            var clientId = _configuration["Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                _logger.LogError("Google:ClientId is not configured. Cannot verify Google ID tokens.");
                throw new InvalidOperationException("Google sign-in is not configured on the server.");
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                };

                payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Google ID token validation failed: invalid or expired token");
                throw new UnauthorizedAccessException("The Google ID token is invalid or has expired.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Could not reach Google to verify the ID token");
                throw new HttpRequestException("Unable to verify your Google account at this time. Please try again later.", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout while contacting Google for token verification");
                throw new HttpRequestException("Unable to verify your Google account at this time. Please try again later.", ex);
            }

            // Require email_verified
            if (!payload.EmailVerified)
            {
                _logger.LogWarning("Google ID token rejected: email not verified for {Email}", payload.Email);
                throw new UnauthorizedAccessException("Your Google email address has not been verified.");
            }

            return new GoogleTokenResult(
                Sub: payload.Subject,
                Email: payload.Email,
                FullName: payload.Name ?? payload.Email
            );
        }
    }
}
