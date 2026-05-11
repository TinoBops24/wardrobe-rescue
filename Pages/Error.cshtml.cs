using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        private readonly ILogger<ErrorModel> _logger;

        // 'new' keyword required — hides PageModel.StatusCode(int) method
        public new int StatusCode { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(int? statusCode = null)
        {
            StatusCode = statusCode ?? 500;

            // Log server errors but not client errors
            if (StatusCode >= 500)
            {
                var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
                if (exceptionFeature != null)
                {
                    _logger.LogError(exceptionFeature.Error,
                        "Unhandled exception on path {Path}", exceptionFeature.Path);
                }
            }

            (Title, Message) = StatusCode switch
            {
                404 => ("Page Not Found",
                        "The page you are looking for does not exist or has been moved."),
                403 => ("Access Denied",
                        "You do not have permission to view this page."),
                500 => ("Server Error",
                        "Something went wrong on our end. Please try again shortly."),
                _ => ("Something Went Wrong",
                        "An unexpected error occurred. Please try again.")
            };
        }
    }
}