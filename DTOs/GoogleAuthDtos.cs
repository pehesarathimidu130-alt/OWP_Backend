using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    /// <summary>
    /// Request payload for POST /api/GoogleAuth/sign-in.
    /// </summary>
    public class GoogleSignInRequest
    {
        [Required(ErrorMessage = "ID token is required.")]
        public string IdToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response for POST /api/GoogleAuth/sign-in.
    /// Status is either "AUTHENTICATED" (auth is populated) or "REGISTRATION_REQUIRED" (prefill is populated).
    /// </summary>
    public class GoogleSignInResponse
    {
        public string Status { get; set; } = string.Empty;

        /// <summary>Present when Status == "AUTHENTICATED".</summary>
        public LoginResponseDto? Auth { get; set; }

        /// <summary>Present when Status == "REGISTRATION_REQUIRED".</summary>
        public GooglePrefill? Prefill { get; set; }
    }

    public class GooglePrefill
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
