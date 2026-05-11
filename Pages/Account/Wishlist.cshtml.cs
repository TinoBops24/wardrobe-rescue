using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Account
{
    public class WishlistModel : PageModel
    {
        private readonly FirestoreService _firestoreService;

        public WishlistModel(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        public List<Product> WishlistProducts { get; set; } = new();
        public string CustomerName { get; set; } = string.Empty;

        // GET 
        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login", new { returnUrl = "/Account/Wishlist" });

            CustomerName = HttpContext.Session.GetString(SessionKeys.UserName) ?? string.Empty;
            ViewData["CartCount"] = HttpContext.Session.GetCartCount();

            var profile = await _firestoreService.GetUserByIdAsync(userId);
            if (profile == null || profile.WishlistProductIds.Count == 0)
                return Page();

            var allProducts = await _firestoreService.GetAllProductsAsync();
            WishlistProducts = allProducts
                .Where(p => profile.WishlistProductIds.Contains(p.Id))
                .ToList();

            return Page();
        }

        // POST: Remove from wishlist 
        public async Task<IActionResult> OnPostRemoveAsync(string productId)
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login");

            var profile = await _firestoreService.GetUserByIdAsync(userId);
            if (profile != null && profile.WishlistProductIds.Contains(productId))
            {
                profile.WishlistProductIds.Remove(productId);
                await _firestoreService.UpdateUserProfileAsync(profile);
                TempData["WishlistSuccess"] = "Item removed from your wishlist.";
            }

            return RedirectToPage();
        }

        // POST: Add to cart from wishlist
        public async Task<IActionResult> OnPostAddToCartAsync(string productId)
        {
            var product = await _firestoreService.GetProductByIdAsync(productId);
            if (product == null)
            {
                TempData["WishlistError"] = "Product not found.";
                return RedirectToPage();
            }

            // Add product to session cart (uses CartHelper.AddToCart)
            HttpContext.Session.AddToCart(product, 1);
            ViewData["CartCount"] = HttpContext.Session.GetCartCount();
            TempData["WishlistSuccess"] = $"{product.Name} added to your cart.";

            return RedirectToPage();
        }
    }
}