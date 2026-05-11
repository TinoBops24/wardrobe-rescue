using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Cart
{
    public class IndexModel : PageModel
    {
        private readonly FirestoreService _firestoreService;

        public IndexModel(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        public List<CartItem> CartItems { get; set; } = new();
        public double CartSubtotal { get; set; }
        public List<Product> SuggestedProducts { get; set; } = new();

        public async Task OnGetAsync()
        {
            CartItems = HttpContext.Session.GetCart();
            CartSubtotal = HttpContext.Session.GetCartTotal();
            ViewData["CartCount"] = HttpContext.Session.GetCartCount();

            await LoadSuggestionsAsync();
        }

        // Remove a single item from the cart
        public async Task<IActionResult> OnPostRemoveAsync(string productId)
        {
            HttpContext.Session.RemoveFromCart(productId);
            TempData["CartSuccess"] = "Item removed from your cart.";
            return RedirectToPage();
        }

        // Update one item's quantity (called per-row via individual form submit)
        public async Task<IActionResult> OnPostUpdateAsync(string productId, int quantity)
        {
            HttpContext.Session.UpdateQuantity(productId, quantity);
            TempData["CartSuccess"] = "Cart updated.";
            return RedirectToPage();
        }

        // Private helpers 

        private async Task LoadSuggestionsAsync()
        {
            var cartProductIds = HttpContext.Session.GetCart()
                .Select(c => c.ProductId)
                .ToHashSet();

            var allProducts = await _firestoreService.GetPublishedProductsAsync();

            SuggestedProducts = allProducts
                .Where(p => !cartProductIds.Contains(p.Id))
                .Take(8)
                .ToList();
        }
    }
}