using Google.Apis.Auth;

namespace Backend.Services
{
    /// <summary>
    /// Verifies a Google ID token and returns the claims we need (sub, email, name).
    /// </summary>
    public interface IGoogleTokenVerifier
    {
        /// <summary>
        /// Validates a Google ID token.
        /// Returns (sub, email, fullName).
        /// Throws:
        ///   UnauthorizedAccessException  → invalid / expired token (401)
        ///   HttpRequestException         → Google unreachable (503)
        /// </summary>
        Task<GoogleTokenResult> VerifyAsync(string idToken);
    }

    public record GoogleTokenResult(string Sub, string Email, string FullName);
}
