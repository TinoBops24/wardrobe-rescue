using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Shared
{
    /// <summary>
    /// Base class for all admin Razor Pages.
    /// Enforces authentication and Admin role before any OnGet/OnPost runs.
    /// </summary>
    public abstract class AdminPageModel : PageModel
    {
        /// <summary>
        /// Override OnPageHandlerExecuting so the guard fires before any
        /// handler method (OnGet, OnPost, etc.) in the derived page.
        /// </summary>
        public override void OnPageHandlerExecuting(
            Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context)
        {
            var session = context.HttpContext.Session;

            var userId = SessionHelper.GetUserId(session);
            var role = SessionHelper.GetRole(session);

            if (string.IsNullOrEmpty(userId))
            {
                // Not logged in — redirect to login, preserving the intended URL
                var returnUrl = context.HttpContext.Request.Path;
                context.Result = new RedirectToPageResult(
                    "/Account/Login",
                    new { returnUrl });
                return;
            }

            if (role != "Admin")
            {
                // Logged in but wrong role
                context.Result = new RedirectToPageResult("/Account/AccessDenied");
                return;
            }

            base.OnPageHandlerExecuting(context);
        }
    }
}