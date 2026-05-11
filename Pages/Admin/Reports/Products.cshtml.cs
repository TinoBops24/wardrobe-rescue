using Google.Cloud.Firestore;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Admin.Reports
{
    public class ProductStatRow
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public double Price { get; set; }
        public double Cost { get; set; }
        public int UnitsSold { get; set; }
        public double RevenueGenerated { get; set; }
    }

    public class ProductsModel : PageModel
    {
        private readonly FirestoreDb _db;

        public ProductsModel(FirestoreDb db) => _db = db;

        public List<ProductStatRow> PagedStats { get; set; } = new();
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public double AveragePrice { get; set; }
        public string TopSellingProduct { get; set; } = "N/A";

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }

        private const int PageSize = 10;

        public async Task OnGetAsync()
        {
            var productSnapshot = await _db.Collection("products")
                .WhereEqualTo("isDraft", false)
                .GetSnapshotAsync();

            var products = productSnapshot.Documents
                .Select(d => d.ConvertTo<Product>())
                .ToList();

            var orderSnapshot = await _db.Collection("orders")
                .GetSnapshotAsync();

            var paidOrders = orderSnapshot.Documents
                .Select(d => d.ConvertTo<Order>())
                .Where(o => o.Status == OrderStatus.Paid)
                .ToList();

            var salesLookup = new Dictionary<string, (int, double)>();
            foreach (var order in paidOrders)
            {
                foreach (var item in order.Items)
                {
                    if (!salesLookup.ContainsKey(item.ProductId))
                        salesLookup[item.ProductId] = (0, 0);

                    var existing = salesLookup[item.ProductId];
                    salesLookup[item.ProductId] = (
                        existing.Item1 + item.Quantity,
                        existing.Item2 + (item.SnapshotPrice * item.Quantity)
                    );
                }
            }

            var allStats = products.Select(p =>
            {
                var sales = salesLookup.ContainsKey(p.Id) ? salesLookup[p.Id] : (0, 0.0);
                return new ProductStatRow
                {
                    Name = p.Name,
                    Category = p.Category,
                    Gender = p.Gender,
                    ImageUrl = p.ImageUrl,
                    Price = p.Price,
                    Cost = p.Cost,
                    UnitsSold = sales.Item1,
                    RevenueGenerated = sales.Item2
                };
            })
            .OrderByDescending(p => p.UnitsSold)
            .ToList();

            TotalProducts = products.Count;
            TotalCategories = products.Select(p => p.Category).Distinct().Count();
            AveragePrice = products.Any() ? products.Average(p => p.Price) : 0;
            TopSellingProduct = allStats.FirstOrDefault(p => p.UnitsSold > 0)?.Name ?? allStats.FirstOrDefault()?.Name ?? "N/A";

            TotalPages = (int)Math.Ceiling(allStats.Count / (double)PageSize);
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages));

            PagedStats = allStats
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }
    }
}