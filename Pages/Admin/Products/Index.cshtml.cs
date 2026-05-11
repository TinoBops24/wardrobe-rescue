using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Admin.Products
{
    public class IndexModel : PageModel
    {
        private readonly FirestoreService _firestore;
        private readonly ILogger<IndexModel> _logger;

        private const int PageSize = 10;

        public List<Product> Products { get; set; } = new();
        public string Filter { get; set; } = "all";
        public string SearchQuery { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int LiveCount { get; set; }
        public int DraftCount { get; set; }
        public int HiddenCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int FilteredCount { get; set; }

        public IndexModel(FirestoreService firestore, ILogger<IndexModel> logger)
        {
            _firestore = firestore;
            _logger = logger;
        }

        public async Task OnGetAsync(string filter = "all", string search = "", int pageNum = 1)
        {
            var all = await _firestore.GetAllProductsAsync();

            TotalCount = all.Count;
            LiveCount = all.Count(p => !p.IsDraft && !p.IsHiddenFromWeb);
            DraftCount = all.Count(p => p.IsDraft);
            HiddenCount = all.Count(p => p.IsHiddenFromWeb && !p.IsDraft);

            Filter = filter;
            SearchQuery = search?.Trim() ?? string.Empty;
            CurrentPage = Math.Max(1, pageNum);

            // Apply status filter
            var filtered = filter switch
            {
                "live" => all.Where(p => !p.IsDraft && !p.IsHiddenFromWeb).ToList(),
                "draft" => all.Where(p => p.IsDraft).ToList(),
                "hidden" => all.Where(p => p.IsHiddenFromWeb && !p.IsDraft).ToList(),
                _ => all
            };

            // Apply fuzzy search if a query is present
            if (!string.IsNullOrWhiteSpace(SearchQuery))
                filtered = FuzzySearch(filtered, SearchQuery);

            FilteredCount = filtered.Count;
            TotalPages = Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
            CurrentPage = Math.Min(CurrentPage, TotalPages);

            Products = filtered
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Invalid product ID.";
                return RedirectToPage(new { filter = "all" });
            }
            try
            {
                await _firestore.DeleteProductAsync(id);
                TempData["Success"] = "Product deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete product {Id}", id);
                TempData["Error"] = "Failed to delete product. Please try again.";
            }
            return RedirectToPage(new { filter = "all" });
        }

        // Fuzzy search — scores each product against the query using a combination
        // of exact substring match, word-level match, and Levenshtein distance.
        // Products scoring above the threshold are returned ordered by relevance.
        private static List<Product> FuzzySearch(List<Product> products, string query)
        {
            var terms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var scored = products
                .Select(p => new { Product = p, Score = ScoreProduct(p, terms) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Product)
                .ToList();

            return scored;
        }

        private static int ScoreProduct(Product p, string[] terms)
        {
            var searchable = new[]
            {
                p.Name.ToLower(),
                p.Category.ToLower(),
                p.Gender.ToLower(),
                string.Join(" ", p.Tags).ToLower()
            };

            var fullText = string.Join(" ", searchable);
            int score = 0;

            foreach (var term in terms)
            {
                // Exact substring match — highest weight
                if (fullText.Contains(term))
                {
                    score += 100;
                    continue;
                }

                // Word-level fuzzy match — check each word in the product text
                var words = fullText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var bestDistance = words
                    .Where(w => Math.Abs(w.Length - term.Length) <= 3)
                    .Select(w => Levenshtein(term, w))
                    .DefaultIfEmpty(int.MaxValue)
                    .Min();

                // Tolerance scales with term length — longer words allow more typos
                var tolerance = term.Length <= 4 ? 1 : term.Length <= 7 ? 2 : 3;

                if (bestDistance <= tolerance)
                    score += Math.Max(10, 60 - bestDistance * 15);
            }

            return score;
        }

        // Standard iterative Levenshtein distance — O(m*n) time, O(n) space
        private static int Levenshtein(string a, string b)
        {
            if (a == b) return 0;
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;

            var prev = Enumerable.Range(0, b.Length + 1).ToArray();
            var curr = new int[b.Length + 1];

            for (int i = 1; i <= a.Length; i++)
            {
                curr[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                Array.Copy(curr, prev, curr.Length);
            }

            return prev[b.Length];
        }
    }
}