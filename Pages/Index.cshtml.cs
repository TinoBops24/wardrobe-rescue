using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages
{
    public class IndexModel : PageModel
    {
        private readonly FirestoreService _firestoreService;

        public IndexModel(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        public List<Product> FeaturedProducts { get; set; } = new();

        public async Task OnGetAsync()
        {
            var all = await _firestoreService.GetPublishedProductsAsync();
            FeaturedProducts = all.Take(6).ToList();
        }
    }
}