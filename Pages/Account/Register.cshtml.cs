using Google.Cloud.Firestore;
using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly FirebaseAuthService _authService;
        private readonly FirestoreService _firestoreService;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            FirebaseAuthService authService,
            FirestoreService firestoreService,
            ILogger<RegisterModel> logger)
        {
            _authService = authService;
            _firestoreService = firestoreService;
            _logger = logger;
        }

        // Only Register fields — nothing else on this page 

        [BindProperty]
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // View state 

        public string? RegisterError { get; set; }

        public string ActiveTab => "register";

        // GET 

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString(SessionKeys.UserId) != null)
                return RedirectToPage("/Index");

            return Page();
        }

        // POST 

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // Create Firebase Auth account
            var authResult = await _authService.SignUpAsync(Email, Password);

            if (!authResult.Succeeded)
            {
                RegisterError = MapFirebaseError(authResult.ErrorMessage);
                _logger.LogWarning("Failed registration attempt for {Email}: {Error}", Email, authResult.ErrorMessage);
                return Page();
            }

            //  Build profile — Id is set by CreateUserProfileAsync (email as document ID)
            var profile = new UserProfile
            {
                FirebaseUid = authResult.FirebaseUid!,
                Email = Email,
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                Role = "Customer",
                IsActive = true,
                CreatedAt = Timestamp.GetCurrentTimestamp()
            };

            try
            {
                await _firestoreService.CreateUserProfileAsync(profile);
                _logger.LogInformation("New account created: {Email}", Email);
            }
            catch (Exception ex)
            {
                RegisterError = "Account creation failed. Please try again.";
                _logger.LogError(ex, "Firestore write failed for new user {Email}", Email);
                return Page();
            }

            // Auto sign-in after registration
            // profile.Id is now correctly set to the email by CreateUserProfileAsync
            WriteSession(profile);

            return RedirectToPage("/Index");
        }

        //Private helpers 

        private void WriteSession(UserProfile profile)
        {
            HttpContext.Session.SetString(SessionKeys.UserId, profile.Id);
            HttpContext.Session.SetString(SessionKeys.UserName, $"{profile.FirstName} {profile.LastName}".Trim());
            HttpContext.Session.SetString(SessionKeys.UserRole, profile.Role);
            HttpContext.Session.SetString(SessionKeys.UserEmail, profile.Email);
        }

        private static string MapFirebaseError(string? errorMessage) =>
            errorMessage?.ToUpperInvariant() switch
            {
                var e when e != null && e.Contains("EMAIL_EXISTS") => "An account with this email address already exists.",
                var e when e != null && e.Contains("WEAK_PASSWORD") => "Password is too weak. Please use at least 6 characters.",
                var e when e != null && e.Contains("TOO_MANY_ATTEMPTS") => "Too many attempts. Please wait a moment and try again.",
                _ => "Something went wrong. Please try again."
            };
    }
}