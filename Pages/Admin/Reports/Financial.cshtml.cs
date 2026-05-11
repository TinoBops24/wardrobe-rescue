using Google.Cloud.Firestore;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Admin.Reports
{
    public class FinancialModel : PageModel
    {
        private readonly FirestoreDb _db;

        public FinancialModel(FirestoreDb db) => _db = db;

        public List<Order> PaidOrders { get; set; } = new();
        public double TotalRevenue { get; set; }
        public double TotalCost { get; set; }
        public double NetProfit { get; set; }
        public double ProfitMargin { get; set; }

        public async Task OnGetAsync()
        {
            var snapshot = await _db.Collection("orders")
                .OrderByDescending("orderDate")
                .GetSnapshotAsync();

            var allOrders = snapshot.Documents
                .Select(d => d.ConvertTo<Order>())
                .ToList();

            PaidOrders = allOrders.Where(o => o.Status == OrderStatus.Paid).ToList();

            TotalRevenue = PaidOrders.Sum(o => o.TotalAmount);
            TotalCost = PaidOrders.Sum(o => o.TotalCost);
            NetProfit = TotalRevenue - TotalCost;
            ProfitMargin = TotalRevenue > 0 ? (NetProfit / TotalRevenue) * 100 : 0;
        }
    }
}