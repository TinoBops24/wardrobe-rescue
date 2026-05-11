using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;
using System.Security.Cryptography;
using System.Text;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Middleware
{
    /// <summary>
    /// Runs on every request after session is available.
    /// If session is empty but a valid remember-me cookie exists,
    /// the session is re-hydrated from Firestore — transparent to the user.
    /// </summary>
    public class RememberMe
    {
        private const string CookieName = "wr_remember";
        private readonly RequestDelegate _next;
        private readonly ILogger<RememberMe> _logger;

        public RememberMe(RequestDelegate next, ILogger<RememberMe> logger)
        {
            _next   = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, FirestoreService firestoreService)
        {
            // Only attempt re-hydration if session has no user
            var userId = context.Session.GetString(SessionKeys.UserId);

            if (string.IsNullOrEmpty(userId))
            {
                var rawToken = context.Request.Cookies[CookieName];

                if (!string.IsNullOrEmpty(rawToken))
                    await TryRehydrateSessionAsync(context, firestoreService, rawToken);
            }

            await _next(context);
        }

        private async Task TryRehydrateSessionAsync(
            HttpContext context,
            FirestoreService firestoreService,
            string rawToken)
        {
            try
            {
                var tokenHash = HashToken(rawToken);
                RememberMeToken? token = await firestoreService.GetRememberMeTokenAsync(tokenHash);

                // Token not found in Firestore — clear stale cookie
                if (token is null)
                {
                    ClearCookie(context);
                    return;
                }

                // Token expired — delete from Firestore and clear cookie
                if (token.ExpiresAt.ToDateTime() < DateTime.UtcNow)
                {
                    _logger.LogInformation(
                        "Remember-me token expired for user {UserId}. Clearing.", token.UserId);
                    await firestoreService.DeleteRememberMeTokenAsync(tokenHash);
                    ClearCookie(context);
                    return;
                }

                // Valid token — re-hydrate session
                context.Session.SetString(SessionKeys.UserId,    token.UserId);
                context.Session.SetString(SessionKeys.UserEmail, token.UserEmail);
                context.Session.SetString(SessionKeys.UserRole,  token.Role);
                context.Session.SetString(SessionKeys.IsAdmin,
                    token.Role == "Admin" ? "true" : "false");

                // Fetch full name from Firestore for display in nav
                UserProfile? profile = await firestoreService.GetUserByIdAsync(token.UserId);
                if (profile is not null)
                {
                    context.Session.SetString(SessionKeys.UserName,
                        $"{profile.FirstName} {profile.LastName}".Trim());
                }

                _logger.LogInformation(
                    "Session re-hydrated from remember-me cookie for user {UserId}.", token.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during remember-me session re-hydration.");
                ClearCookie(context);
            }
        }

        private static void ClearCookie(HttpContext context)
        {
            context.Response.Cookies.Delete(CookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure   = true,
                SameSite = SameSiteMode.Lax
            });
        }

        /// <summary>
        /// SHA-256 hash of the raw token — only the hash is stored in Firestore.
        /// If Firestore is ever breached, raw tokens are not exposed.
        /// </summary>
        public static string HashToken(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}