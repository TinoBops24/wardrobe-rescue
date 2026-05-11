using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Account
{
    public class OrdersModel : PageModel
    {
        private readonly FirestoreService _firestoreService;

        public OrdersModel(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        public List<Order> Orders { get; set; } = new();
        public string CustomerName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login", new { returnUrl = "/Account/Orders" });

            CustomerName = HttpContext.Session.GetString(SessionKeys.UserName) ?? string.Empty;
            ViewData["CartCount"] = HttpContext.Session.GetCartCount();

            Orders = await _firestoreService.GetOrdersByUserIdAsync(userId);

            return Page();
        }
    }
}