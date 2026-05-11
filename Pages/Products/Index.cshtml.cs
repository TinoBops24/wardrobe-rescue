using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Products
{
    public class IndexModel : PageModel
    {
        private readonly FirestoreService _firestoreService;

        public IndexModel(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        public List<Product> Products { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public List<string> Genders { get; set; } = new();
        public List<string> WishlistProductIds { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? SearchQuery { get; set; }
        [BindProperty(SupportsGet = true)] public string? SelectedCategory { get; set; }
        [BindProperty(SupportsGet = true)] public string? SelectedGender { get; set; }
        [BindProperty(SupportsGet = true)] public double? MinPrice { get; set; }
        [BindProperty(SupportsGet = true)] public double? MaxPrice { get; set; }

        // GET 
        public async Task OnGetAsync()
        {
            var all = await _firestoreService.GetPublishedProductsAsync();

            Categories = all
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            Genders = all
                .Select(p => p.Gender)
                .Where(g => !string.IsNullOrEmpty(g))
                .Distinct()
                .OrderBy(g => g)
                .ToList();

            var filtered = all.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var q = SearchQuery.ToLower();
                filtered = filtered.Where(p =>
                    p.Name.ToLower().Contains(q) ||
                    (p.Description?.ToLower().Contains(q) == true) ||
                    p.Tags.Any(t => t.ToLower().Contains(q)));
            }

            if (!string.IsNullOrWhiteSpace(SelectedCategory))
                filtered = filtered.Where(p =>
                    p.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(SelectedGender))
                filtered = filtered.Where(p =>
                    p.Gender.Equals(SelectedGender, StringComparison.OrdinalIgnoreCase));

            if (MinPrice.HasValue)
                filtered = filtered.Where(p => (p.DiscountPrice ?? p.Price) >= MinPrice.Value);

            if (MaxPrice.HasValue)
                filtered = filtered.Where(p => (p.DiscountPrice ?? p.Price) <= MaxPrice.Value);

            Products = filtered.ToList();
            ViewData["CartCount"] = HttpContext.Session.GetCartCount();

            // Load wishlist IDs if logged in — used to show filled/empty heart
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);
            if (!string.IsNullOrEmpty(userId))
            {
                var profile = await _firestoreService.GetUserByIdAsync(userId);
                WishlistProductIds = profile?.WishlistProductIds ?? new();
            }
        }

        // POST: Toggle wishlist 
        public async Task<IActionResult> OnPostToggleWishlistAsync(string productId)
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);

            // Not logged in — redirect to login then back
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login", new { returnUrl = "/Products/Index" });

            await _firestoreService.ToggleWishlistAsync(userId, productId);

            return RedirectToPage(new
            {
                SearchQuery,
                SelectedCategory,
                SelectedGender,
                MinPrice,
                MaxPrice
            });
        }
    }
}