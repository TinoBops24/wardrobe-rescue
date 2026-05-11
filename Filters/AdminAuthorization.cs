using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Filters
{
    /// <summary>
    /// Applied globally to all /Admin/* pages via Program.cs page conventions.
    ///
    /// Unauthenticated users and brute-force attempts receive a 404 —
    /// the admin area is completely invisible to anyone not logged in.
    ///
    /// Logged-in customers receive a redirect to /Account/AccessDenied —
    /// this makes role-based authorisation visible to the panel without
    /// revealing that the admin area exists to unauthenticated visitors.
    ///
    /// Implements IFilterFactory so ASP.NET's DI container resolves the
    /// logger correctly — no manual service provider construction needed.
    /// </summary>
    public class AdminAuthorization : IFilterFactory
    {
        public bool IsReusable => false;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider
                .GetRequiredService<ILogger<AdminAuthorizationImpl>>();
            return new AdminAuthorizationImpl(logger);
        }

        // ── Inner implementation ──────────────────────────────────────────
        public class AdminAuthorizationImpl : IAsyncPageFilter
        {
            private readonly ILogger<AdminAuthorizationImpl> _logger;

            public AdminAuthorizationImpl(ILogger<AdminAuthorizationImpl> logger)
            {
                _logger = logger;
            }

            public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
                => Task.CompletedTask;

            public async Task OnPageHandlerExecutionAsync(
                PageHandlerExecutingContext context,
                PageHandlerExecutionDelegate next)
            {
                var session = context.HttpContext.Session;
                var isAdmin = session.GetString(SessionKeys.IsAdmin);
                var userId = session.GetString(SessionKeys.UserId);

                // ── Admin: allow through ──────────────────────────────────
                if (isAdmin == "true")
                {
                    await next();
                    return;
                }

                // ── Logged-in customer: redirect to Access Denied ─────────
                // Demonstrates role-based authorisation clearly to the panel.
                if (!string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning(
                        "Customer attempted to access admin path {Path}. UserId: {UserId}.",
                        context.HttpContext.Request.Path,
                        userId);

                    context.Result = new RedirectToPageResult("/Account/AccessDenied");
                    return;
                }

                // ── Unauthenticated / brute-force: silent 404 ─────────────
                // Nothing confirms the admin area exists.
                _logger.LogWarning(
                    "Unauthenticated request blocked at admin path {Path}.",
                    context.HttpContext.Request.Path);

                context.Result = new NotFoundResult();
            }
        }
    }
}