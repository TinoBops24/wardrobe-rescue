using Google.Cloud.Firestore;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Admin.Orders
{
    public class IndexModel : PageModel
    {
        private readonly FirestoreDb _db;

        public IndexModel(FirestoreDb db)
        {
            _db = db;
        }

        public List<Order> Orders { get; set; } = new();
        public int TotalCount { get; set; }
        public int PaidCount { get; set; }
        public int CancelledCount { get; set; }
        public double TotalRevenue { get; set; }
        public double TotalProfit { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Filter { get; set; } = "all";

        public async Task OnGetAsync()
        {
            await LoadOrdersAsync();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(string orderId, string newStatus, string filter)
        {
            if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(newStatus))
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToPage(new { filter });
            }

            try
            {
                var docRef = _db.Collection("orders").Document(orderId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToPage(new { filter });
                }

                var statusValue = newStatus == "Paid" ? (int)OrderStatus.Paid : (int)OrderStatus.Cancelled;

                await docRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "status", statusValue }
                });

                TempData["Success"] = $"Order {orderId} has been marked as {newStatus}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to update order: {ex.Message}";
            }

            return RedirectToPage(new { filter });
        }

        private async Task LoadOrdersAsync()
        {
            var snapshot = await _db.Collection("orders")
                .OrderByDescending("orderDate")
                .GetSnapshotAsync();

            var all = snapshot.Documents
                .Select(d => d.ConvertTo<Order>())
                .ToList();

            TotalCount = all.Count;
            PaidCount = all.Count(o => o.Status == OrderStatus.Paid);
            CancelledCount = all.Count(o => o.Status == OrderStatus.Cancelled);
            TotalRevenue = all.Where(o => o.Status == OrderStatus.Paid).Sum(o => o.TotalAmount);
            TotalProfit = all.Where(o => o.Status == OrderStatus.Paid).Sum(o => o.TotalAmount - o.TotalCost);

            Orders = Filter switch
            {
                "paid" => all.Where(o => o.Status == OrderStatus.Paid).ToList(),
                "cancelled" => all.Where(o => o.Status == OrderStatus.Cancelled).ToList(),
                _ => all
            };
        }
    }
}