using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Checkout
{
    public class SuccessModel : PageModel
    {
        private readonly FirestoreService _firestoreService;

        public SuccessModel(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        public Order Order { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Must be logged in
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login");

            // Grab the orderId set by IndexModel before the redirect
            var orderId = TempData["OrderId"]?.ToString();
            if (string.IsNullOrEmpty(orderId))
                return RedirectToPage("/Index");

            // Fetch the full order from Firestore so the view has everything
            var order = await _firestoreService.GetOrderByIdAsync(orderId);
            if (order is null || order.UserId != userId)
                return RedirectToPage("/Index");

            Order = order;
            return Page();
        }
    }
}