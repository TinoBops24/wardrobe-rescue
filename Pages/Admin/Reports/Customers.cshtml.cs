using Google.Cloud.Firestore;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Admin.Reports
{
    public class CustomerStatRow
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public double TotalSpent { get; set; }
        public double AverageOrderValue => TotalOrders > 0 ? TotalSpent / TotalOrders : 0;
        public DateTime? LastOrderDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class CustomersModel : PageModel
    {
        private readonly FirestoreDb _db;

        public CustomersModel(FirestoreDb db) => _db = db;

        public List<CustomerStatRow> CustomerStats { get; set; } = new();
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public double AverageOrderValue { get; set; }
        public string TopCustomerName { get; set; } = "N/A";

        public async Task OnGetAsync()
        {
            var userSnapshot = await _db.Collection("userProfiles")
                .WhereEqualTo("role", "Customer")
                .GetSnapshotAsync();

            var users = userSnapshot.Documents
                .Select(d => d.ConvertTo<UserProfile>())
                .ToList();

            var orderSnapshot = await _db.Collection("orders")
                .GetSnapshotAsync();

            var paidOrders = orderSnapshot.Documents
                .Select(d => d.ConvertTo<Order>())
                .Where(o => o.Status == OrderStatus.Paid)
                .ToList();

            var ordersByEmail = paidOrders
                .GroupBy(o => o.CustomerEmail)
                .ToDictionary(g => g.Key, g => g.ToList());

            CustomerStats = users.Select(u =>
            {
                var email = u.Email ?? string.Empty;
                var userOrders = ordersByEmail.ContainsKey(email) ? ordersByEmail[email] : new List<Order>();
                var lastOrder = userOrders.Any()
                    ? (DateTime?)userOrders.Max(o => o.OrderDate.ToDateTime())
                    : null;

                return new CustomerStatRow
                {
                    FullName = $"{u.FirstName} {u.LastName}".Trim(),
                    Email = email,
                    TotalOrders = userOrders.Count,
                    TotalSpent = userOrders.Sum(o => o.TotalAmount),
                    LastOrderDate = lastOrder,
                    IsActive = u.IsActive
                };
            })
            .OrderByDescending(c => c.TotalSpent)
            .ToList();

            TotalCustomers = users.Count;
            ActiveCustomers = users.Count(u => u.IsActive);
            AverageOrderValue = paidOrders.Any() ? paidOrders.Average(o => o.TotalAmount) : 0;
            TopCustomerName = CustomerStats.FirstOrDefault(c => c.TotalOrders > 0)?.FullName ?? "N/A";
        }
    }
}