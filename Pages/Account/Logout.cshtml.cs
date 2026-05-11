using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;
using INF4027W_BPTTIN002_MiniPrj_2026.Middleware;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private const string CookieName = "wr_remember";

        private readonly FirestoreService _firestoreService;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(FirestoreService firestoreService, ILogger<LogoutModel> logger)
        {
            _firestoreService = firestoreService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);

            // Delete the remember-me token from Firestore for this device
            var rawToken = Request.Cookies[CookieName];
            if (!string.IsNullOrEmpty(rawToken))
            {
                try
                {
                    var tokenHash = RememberMe.HashToken(rawToken);
                    await _firestoreService.DeleteRememberMeTokenAsync(tokenHash);
                    _logger.LogInformation(
                        "Remember-me token deleted for user {UserId}.", userId);
                }
                catch (Exception ex)
                {
                    // Non-fatal — session and cookie are still cleared below
                    _logger.LogError(ex, "Failed to delete remember-me token for user {UserId}.", userId);
                }

                // Clear the cookie from the browser
                Response.Cookies.Delete(CookieName, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax
                });
            }

            // Clear all session data
            HttpContext.Session.Clear();

            _logger.LogInformation("User {UserId} signed out.", userId);

            return RedirectToPage("/Index");
        }
    }
}