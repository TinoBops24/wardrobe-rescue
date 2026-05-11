using Google.Cloud.Firestore;
using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;
using INF4027W_BPTTIN002_MiniPrj_2026.Middleware;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly FirebaseAuthService _authService;
        private readonly FirestoreService _firestoreService;
        private readonly ILogger<LoginModel> _logger;

        private const string CookieName = "wr_remember";
        private const int RememberMeDays = 7;

        public LoginModel(
            FirebaseAuthService authService,
            FirestoreService firestoreService,
            ILogger<LoginModel> logger)
        {
            _authService = authService;
            _firestoreService = firestoreService;
            _logger = logger;
        }

       
        [BindProperty]
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public bool RememberMeChecked { get; set; } = false;

        // View state 

        public string? LoginError { get; set; }
        public string ReturnUrl { get; set; } = "/";
        public string ActiveTab => "login";

        // GET

        public IActionResult OnGet(string? returnUrl = null)
        {
            if (HttpContext.Session.GetString(SessionKeys.UserId) != null)
                return RedirectToPage("/Index");

            ReturnUrl = returnUrl ?? "/";
            return Page();
        }

        // POST 

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? "/";

            if (!ModelState.IsValid)
                return Page();

            // Authenticate against Firebase Auth REST API
            var authResult = await _authService.SignInAsync(Email, Password);

            if (!authResult.Succeeded)
            {
                LoginError = MapFirebaseError(authResult.ErrorMessage);
                _logger.LogWarning("Failed sign-in attempt for {Email}: {Error}", Email, authResult.ErrorMessage);
                return Page();
            }

            //  Load UserProfile from Firestore by Firebase UID
            var profile = await _firestoreService.GetUserByFirebaseUidAsync(authResult.FirebaseUid!);

            if (profile == null)
            {
                LoginError = "Account setup is incomplete. Please contact support.";
                _logger.LogError("No UserProfile found for FirebaseUid {Uid} (email: {Email})",
                    authResult.FirebaseUid, Email);
                return Page();
            }

            if (!profile.IsActive)
            {
                LoginError = "Your account has been deactivated. Please contact support.";
                _logger.LogWarning("Deactivated account sign-in attempt: {Email}", Email);
                return Page();
            }

            //Write session
            WriteSession(profile);

            _logger.LogInformation(
                "User {Email} signed in successfully. Role: {Role}", Email, profile.Role);

            //  Remember Me — issue persistent token if ticked
            if (RememberMeChecked)
                await IssueRememberMeTokenAsync(profile);

            // Role-aware redirect
            // Admin always goes to dashboard, ignoring returnUrl
            if (profile.Role == "Admin")
                return RedirectToPage("/Admin/Dashboard");

            // Honour returnUrl for customers (covers checkout redirect)
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToPage("/Index");
        }

        // Private helpers 

        private void WriteSession(UserProfile profile)
        {
            HttpContext.Session.SetString(SessionKeys.UserId, profile.Id);
            HttpContext.Session.SetString(SessionKeys.UserName, $"{profile.FirstName} {profile.LastName}".Trim());
            HttpContext.Session.SetString(SessionKeys.UserRole, profile.Role);
            HttpContext.Session.SetString(SessionKeys.UserEmail, profile.Email);
            HttpContext.Session.SetString(SessionKeys.IsAdmin, profile.Role == "Admin" ? "true" : "false");
        }

        private async Task IssueRememberMeTokenAsync(UserProfile profile)
        {
            try
            {
                // Generate a cryptographically secure random token
                var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

                // Use full namespace to avoid conflict with RememberMeChecked property
                var tokenHash = Middleware.RememberMe.HashToken(rawToken);

                var token = new RememberMeToken
                {
                    Id = tokenHash,
                    UserId = profile.Id,
                    UserEmail = profile.Email,
                    Role = profile.Role,
                    ExpiresAt = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(RememberMeDays)),
                    CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
                    DeviceHint = HttpContext.Request.Headers.UserAgent.ToString()
                                    [..Math.Min(200, HttpContext.Request.Headers.UserAgent.ToString().Length)]
                };

                await _firestoreService.CreateRememberMeTokenAsync(token);

                // Write the raw token (not the hash) to the browser cookie
                Response.Cookies.Append(CookieName, rawToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(RememberMeDays)
                });

                _logger.LogInformation(
                    "Remember-me token issued for user {UserId}. Expires in {Days} days.",
                    profile.Id, RememberMeDays);
            }
            catch (Exception ex)
            {
                // Non-fatal — user is still logged in via session
                _logger.LogError(ex, "Failed to issue remember-me token for {UserId}.", profile.Id);
            }
        }

        private static string MapFirebaseError(string? errorMessage) =>
            errorMessage?.ToUpperInvariant() switch
            {
                var e when e != null && e.Contains("EMAIL_NOT_FOUND") => "No account found with that email address.",
                var e when e != null && e.Contains("INVALID_PASSWORD") => "Incorrect password. Please try again.",
                var e when e != null && e.Contains("USER_DISABLED") => "This account has been disabled. Please contact support.",
                var e when e != null && e.Contains("TOO_MANY_ATTEMPTS") => "Too many attempts. Please wait a moment and try again.",
                var e when e != null && e.Contains("INVALID_LOGIN_CREDENTIALS") => "Invalid email or password.",
                _ => "Something went wrong. Please try again."
            };
    }
}