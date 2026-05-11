using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Products
{
    public class DetailsModel : PageModel
    {
        private readonly FirestoreService _firestoreService;

        public DetailsModel(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        //  Displayed data 
        public Product? Product { get; set; }
        public List<Product> RelatedProducts { get; set; } = new();
        public bool IsWishlisted { get; set; } = false;
        public List<string> WishlistProductIds { get; set; } = new();

        // Form bindings g? SelectedSize { get; set; }
        [BindProperty] public int Quantity { get; set; } = 1;

        // GET 
        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            Product = await _firestoreService.GetProductByIdAsync(id);
            if (Product == null)
                return NotFound();

            var all = await _firestoreService.GetPublishedProductsAsync();
            RelatedProducts = all
                .Where(p => p.Category == Product.Category && p.Id != Product.Id)
                .Take(8)
                .ToList();

            ViewData["CartCount"] = HttpContext.Session.GetCartCount();

            // Load wishlist state if logged in
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);
            if (!string.IsNullOrEmpty(userId))
            {
                var profile = await _firestoreService.GetUserByIdAsync(userId);
                WishlistProductIds = profile?.WishlistProductIds ?? new();
                IsWishlisted = WishlistProductIds.Contains(Product.Id);
            }

            return Page();
        }

        // POST: Add to Cart
        public async Task<IActionResult> OnPostAddToCartAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var product = await _firestoreService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            var qty = Math.Max(1, Quantity);
            HttpContext.Session.AddToCart(product, qty);

            TempData["CartSuccess"] = $"'{product.Name}' added to your cart.";
            return RedirectToPage("/Products/Details", new { id });
        }

        // POST: Toggle wishlist 
        public async Task<IActionResult> OnPostToggleWishlistAsync(string id)
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login", new { returnUrl = $"/Products/{id}" });

            await _firestoreService.ToggleWishlistAsync(userId, id);
            return RedirectToPage("/Products/Details", new { id });
        }
    }
}