using Backend.Constants;
using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Backend.Services
{
    public class VendorRegistrationService : IVendorRegistrationService
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;
        private readonly INotificationService _notificationService;
        private readonly IGoogleTokenVerifier _googleTokenVerifier;
        private readonly ILogger<VendorRegistrationService> _logger;

        // ── Single source of truth: the 25 Sri Lankan districts ──
        public static readonly HashSet<string> ValidDistricts = new(StringComparer.OrdinalIgnoreCase)
        {
            "Ampara", "Anuradhapura", "Badulla", "Batticaloa", "Colombo",
            "Galle", "Gampaha", "Hambantota", "Jaffna", "Kalutara",
            "Kandy", "Kegalle", "Kilinochchi", "Kurunegala", "Mannar",
            "Matale", "Matara", "Monaragala", "Mullaitivu", "Nuwara Eliya",
            "Polonnaruwa", "Puttalam", "Ratnapura", "Trincomalee", "Vavuniya"
        };

        private static readonly HashSet<string> ValidBusinessTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Individual", "Partnership", "PrivateLimited", "Other"
        };

        // Phone regex: after stripping spaces/hyphens, must match +94XXXXXXXXX or 0XXXXXXXXX
        private static readonly Regex PhoneRegex = new(@"^(\+94|0)\d{9}$", RegexOptions.Compiled);

        public VendorRegistrationService(
            AppDbContext context,
            IAuthService authService,
            INotificationService notificationService,
            IGoogleTokenVerifier googleTokenVerifier,
            ILogger<VendorRegistrationService> logger)
        {
            _context = context;
            _authService = authService;
            _notificationService = notificationService;
            _googleTokenVerifier = googleTokenVerifier;
            _logger = logger;
        }

        public async Task<RegistrationOptionsResponse> GetOptionsAsync()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.CategoryId)
                .Select(c => c.CategoryName)
                .ToListAsync();

            return new RegistrationOptionsResponse
            {
                Categories = categories,
                Districts = ValidDistricts.OrderBy(d => d).ToList(),
                BusinessTypes = new List<BusinessTypeOption>
                {
                    new() { Value = "Individual",     Label = "Individual / Sole proprietor" },
                    new() { Value = "Partnership",    Label = "Partnership" },
                    new() { Value = "PrivateLimited", Label = "Private Limited Company" },
                    new() { Value = "Other",          Label = "Other" }
                }
            };
        }

        public async Task<LoginResponseDto> RegisterAsync(VendorRegistrationRequest request)
        {
            // ── 0. Google flow: verify token and override email/name ──
            bool isGoogleFlow = !string.IsNullOrWhiteSpace(request.GoogleIdToken);
            GoogleTokenResult? googleResult = null;

            if (isGoogleFlow)
            {
                // Verify the token (throws UnauthorizedAccessException or HttpRequestException)
                googleResult = await _googleTokenVerifier.VerifyAsync(request.GoogleIdToken!);

                // Check if this Google subject is already linked to an account
                var existingLink = await _context.UserExternalLogins
                    .AnyAsync(uel => uel.Provider == "Google" && uel.ProviderSubject == googleResult.Sub);
                if (existingLink)
                {
                    throw new DuplicateEmailException(googleResult.Email,
                        "This Google account is already linked to an existing account.");
                }
            }

            // ── 1. Normalize (Google flow: email and fullName come from the token) ──
            var fullName = isGoogleFlow ? googleResult!.FullName.Trim() : (request.FullName?.Trim() ?? "");
            var email = isGoogleFlow ? googleResult!.Email.ToLower().Trim() : (request.Email?.Trim().ToLower() ?? "");
            var phoneNumber = NormalizePhone(request.PhoneNumber);
            var contactNumber = NormalizePhone(request.ContactNumber);
            var altPhoneNumber = string.IsNullOrWhiteSpace(request.AltPhoneNumber)
                ? null
                : NormalizePhone(request.AltPhoneNumber);
            var businessName = request.BusinessName?.Trim() ?? "";
            var businessType = request.BusinessType?.Trim() ?? "";
            var category = request.Category?.Trim() ?? "";
            var tagline = request.Tagline?.Trim();
            var description = request.Description?.Trim() ?? "";
            var businessEmail = request.BusinessEmail?.Trim().ToLower() ?? "";
            var websiteUrl = request.WebsiteUrl?.Trim();
            var address = request.Address?.Trim() ?? "";
            var city = request.City?.Trim() ?? "";
            var district = request.District?.Trim() ?? "";
            var postalCode = request.PostalCode?.Trim();
            var businessRegNumber = request.BusinessRegistrationNumber?.Trim();
            var serviceAreas = (request.ServiceAreas ?? Array.Empty<string>())
                .Select(s => s?.Trim() ?? "")
                .Where(s => s.Length > 0)
                .ToList();

            // ── 2. Service-level validation (beyond DataAnnotations) ──
            var errors = new Dictionary<string, string[]>();

            // Password required only when NOT Google flow
            if (!isGoogleFlow)
            {
                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    errors["password"] = new[] { "Password is required." };
                }
                else if (!IsValidPassword(request.Password))
                {
                    errors["password"] = new[] { "Password must be at least 8 characters and contain an uppercase letter, a lowercase letter, and a digit." };
                }
            }

            // Phone validations
            if (!PhoneRegex.IsMatch(phoneNumber))
            {
                errors["phoneNumber"] = new[] { "Phone number must be a valid Sri Lankan number (e.g. +94XXXXXXXXX or 0XXXXXXXXX)." };
            }

            if (!PhoneRegex.IsMatch(contactNumber))
            {
                errors["contactNumber"] = new[] { "Contact number must be a valid Sri Lankan number." };
            }

            if (altPhoneNumber != null && !PhoneRegex.IsMatch(altPhoneNumber))
            {
                errors["altPhoneNumber"] = new[] { "Alternate phone number must be a valid Sri Lankan number." };
            }

            // Business type
            if (!ValidBusinessTypes.Contains(businessType))
            {
                errors["businessType"] = new[] { $"Business type must be one of: {string.Join(", ", ValidBusinessTypes)}." };
            }

            // Category must exist in DB
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryName == category);
            if (!categoryExists)
            {
                errors["category"] = new[] { "The selected category does not exist." };
            }

            // District
            if (!ValidDistricts.Contains(district))
            {
                errors["district"] = new[] { "The selected district is not valid." };
            }

            // Service areas
            if (serviceAreas.Count == 0)
            {
                errors["serviceAreas"] = new[] { "At least one service area is required." };
            }
            else
            {
                var invalidAreas = serviceAreas.Where(a => !ValidDistricts.Contains(a)).ToList();
                if (invalidAreas.Count > 0)
                {
                    errors["serviceAreas"] = new[] { $"Invalid service area(s): {string.Join(", ", invalidAreas)}." };
                }
            }

            // Website URL format
            if (!string.IsNullOrWhiteSpace(websiteUrl)
                && !websiteUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !websiteUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                errors["websiteUrl"] = new[] { "Website URL must start with http:// or https://." };
            }

            // Terms
            if (!request.AcceptTerms)
            {
                errors["acceptTerms"] = new[] { "You must accept the terms and conditions." };
            }

            if (errors.Count > 0)
            {
                throw new ValidationException(errors);
            }

            // ── 3. Check email uniqueness (case-insensitive) ──
            var emailTaken = await _context.Users.AnyAsync(u => u.Email.ToLower() == email);
            if (emailTaken)
            {
                throw new DuplicateEmailException(email);
            }

            // ── 4. Look up the Vendor role ──
            var vendorRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName.ToLower() == "vendor")
                ?? throw new InvalidOperationException("Vendor role not found in the database.");

            // ── 5. Atomic transaction: User + Vendor + (UserExternalLogin) + Notification ──
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Build password hash: real password for password flow, random bytes for Google flow
                string passwordHash;
                if (isGoogleFlow)
                {
                    // 32 cryptographically random bytes → BCrypt hash → account cannot be entered via password form
                    var randomBytes = RandomNumberGenerator.GetBytes(32);
                    passwordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(randomBytes));
                }
                else
                {
                    passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                }

                // Create User
                var user = new User
                {
                    RoleId = vendorRole.RoleId,
                    FullName = fullName,
                    Email = email,
                    PasswordHash = passwordHash,
                    PhoneNumber = phoneNumber,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create UserExternalLogin for Google flow
                if (isGoogleFlow)
                {
                    _context.UserExternalLogins.Add(new UserExternalLogin
                    {
                        UserId = user.UserId,
                        Provider = "Google",
                        ProviderSubject = googleResult!.Sub
                    });
                    await _context.SaveChangesAsync();
                }

                // Create Vendor
                var vendor = new Vendor
                {
                    UserId = user.UserId,
                    OwnerName = fullName,
                    BusinessName = businessName,
                    BusinessType = businessType,
                    Category = category,
                    Tagline = string.IsNullOrWhiteSpace(tagline) ? null : tagline,
                    Description = description,
                    YearsInBusiness = request.YearsInBusiness,
                    BusinessRegistrationNumber = string.IsNullOrWhiteSpace(businessRegNumber) ? null : businessRegNumber,
                    Email = businessEmail,
                    ContactNumber = contactNumber,
                    AltPhoneNumber = altPhoneNumber,
                    WebsiteUrl = string.IsNullOrWhiteSpace(websiteUrl) ? null : websiteUrl,
                    Address = address,
                    City = city,
                    State = district,
                    PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode,
                    Country = "Sri Lanka",
                    ServiceAreas = string.Join(", ", serviceAreas),
                    TermsAcceptedAt = DateTime.UtcNow,
                    // System-set pending values
                    IsApproved = false,
                    Status = "Pending",
                    VerificationStatus = "Pending"
                };

                _context.Vendors.Add(vendor);
                await _context.SaveChangesAsync();

                // Notify all admins (uses its own SaveChangesAsync internally, but participates in our transaction)
                await _notificationService.CreateForAllAdminsAsync(
                    NotificationTypes.VendorRegistered,
                    "New vendor registration",
                    $"{businessName} has registered as a new vendor. Please review and verify their identity and account.");

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Vendor registered successfully ({Flow}): UserId={UserId}, BusinessName={BusinessName}",
                    isGoogleFlow ? "Google" : "Password", user.UserId, businessName);

                // ── 6. Build login response (after commit, so UserId is final) ──
                return _authService.BuildVendorLoginResponse(user);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                // Race condition: another request inserted the same email or Google sub between our check and insert
                await transaction.RollbackAsync();
                throw new DuplicateEmailException(email);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ── Helpers ──

        private static string NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "";
            return phone.Replace(" ", "").Replace("-", "");
        }

        private static bool IsValidPassword(string password)
        {
            if (password.Length < 8) return false;
            bool hasUpper = false, hasLower = false, hasDigit = false;
            foreach (var c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsLower(c)) hasLower = true;
                else if (char.IsDigit(c)) hasDigit = true;
            }
            return hasUpper && hasLower && hasDigit;
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            // PostgreSQL error code 23505 = unique_violation
            var inner = ex.InnerException;
            return inner != null && inner.Message.Contains("23505");
        }
    }

    // ── Custom exception types for the controller to map to HTTP responses ──

    /// <summary>
    /// Thrown when service-level validation fails (multiple field errors).
    /// </summary>
    public class ValidationException : Exception
    {
        public Dictionary<string, string[]> Errors { get; }
        public ValidationException(Dictionary<string, string[]> errors)
            : base("One or more validation errors occurred.")
        {
            Errors = errors;
        }
    }

    /// <summary>
    /// Thrown when the email is already registered (409 Conflict).
    /// </summary>
    public class DuplicateEmailException : Exception
    {
        public string EmailAddress { get; }
        public DuplicateEmailException(string email)
            : base($"The email address '{email}' is already registered.")
        {
            EmailAddress = email;
        }

        public DuplicateEmailException(string email, string message)
            : base(message)
        {
            EmailAddress = email;
        }
    }
}
