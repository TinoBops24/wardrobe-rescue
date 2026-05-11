using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Account
{
    public class ProfileModel : PageModel
    {
        private readonly FirestoreService _firestoreService;

        public ProfileModel(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        [BindProperty]
        public ProfileInputModel Input { get; set; } = new();

        public string CustomerName { get; set; } = string.Empty;

        public class ProfileInputModel
        {
            [Required(ErrorMessage = "First name is required.")]
            [Display(Name = "First Name")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Last name is required.")]
            [Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login", new { returnUrl = "/Account/Profile" });

            CustomerName = HttpContext.Session.GetString(SessionKeys.UserName) ?? string.Empty;
            ViewData["CartCount"] = HttpContext.Session.GetCartCount();

            var profile = await _firestoreService.GetUserByIdAsync(userId);
            if (profile != null)
            {
                Input.FirstName = profile.FirstName;
                Input.LastName = profile.LastName;
                Input.Email = profile.Email;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login");

            CustomerName = HttpContext.Session.GetString(SessionKeys.UserName) ?? string.Empty;
            ViewData["CartCount"] = HttpContext.Session.GetCartCount();

            if (!ModelState.IsValid)
                return Page();

            var profile = await _firestoreService.GetUserByIdAsync(userId);
            if (profile == null)
            {
                TempData["ProfileError"] = "Profile not found.";
                return Page();
            }

            profile.FirstName = Input.FirstName;
            profile.LastName = Input.LastName;

            await _firestoreService.UpdateUserProfileAsync(profile);

            // Update session display name
            HttpContext.Session.SetString(SessionKeys.UserName, $"{profile.FirstName} {profile.LastName}");

            TempData["ProfileSuccess"] = "Your profile has been updated successfully.";
            return RedirectToPage();
        }
    }
}