using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        
        // Dependencies
        private readonly FirestoreService _firestoreService;

        public DashboardModel(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }


        public string Greeting { get; private set; } = string.Empty;
        public string DisplayName { get; private set; } = string.Empty;

        public int TotalProducts { get; private set; }
        public int TotalBundles { get; private set; }
        public int TotalOrders { get; private set; }
        public int TotalCustomers { get; private set; }

        public double TotalRevenue { get; private set; }
        public double TotalCostOfSales { get; private set; }
        public double NetProfit { get; private set; }

        public List<Order> RecentOrders { get; private set; } = new();

       
        // Handlers 
        public async Task<IActionResult> OnGetAsync()
        {
            SetGreeting();
            await LoadDashboardDataAsync();
            return Page();
        }

        // Private Helpers
        

        private void SetGreeting()
        {
            Greeting = DateTime.Now.Hour switch
            {
                < 12 => "Good Morning",
                < 17 => "Good Afternoon",
                _ => "Good Evening"
            };

            DisplayName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Admin";
        }

        private async Task LoadDashboardDataAsync()
        {
            // Fire all Firestore reads concurrently to minimise latency
            var productsTask = _firestoreService.GetAllProductsAsync();
            var bundlesTask = _firestoreService.GetAllBundlesAsync();
            var ordersTask = _firestoreService.GetAllOrdersAsync();
            var usersTask = _firestoreService.GetAllUserProfilesAsync();

            await Task.WhenAll(productsTask, bundlesTask, ordersTask, usersTask);

            var products = await productsTask;
            var bundles = await bundlesTask;
            var orders = await ordersTask;
            var users = await usersTask;

            // General stats
            TotalProducts = products.Count(p => !p.IsDraft && !p.IsHiddenFromWeb);
            TotalBundles = bundles.Count;
            TotalOrders = orders.Count;
            TotalCustomers = users.Count(u => u.Role == "Customer");

            // Financial report — Paid orders only
            var paidOrders = orders.Where(o => o.Status == OrderStatus.Paid).ToList();
            TotalRevenue = paidOrders.Sum(o => o.TotalAmount);
            TotalCostOfSales = paidOrders.Sum(o => o.TotalCost);
            NetProfit = TotalRevenue - TotalCostOfSales;

            // Recent orders — already sorted descending by GetAllOrdersAsync()
            RecentOrders = orders.Take(5).ToList();
        }
    }
}