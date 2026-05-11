using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Checkout
{
    public class IndexModel : PageModel
    {
        private readonly FirestoreService _firestoreService;

        public IndexModel(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        public List<CartItem> CartItems { get; set; } = new();
        public double CartSubtotal { get; set; }

        //  Visible required fields

        [BindProperty]
        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Delivery address is required.")]
        public string Address { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Phone number is required.")]
        public string Phone { get; set; } = string.Empty;

        [BindProperty]
        public string? OrderNotes { get; set; }

        [BindProperty]
        public string ShippingMethod { get; set; } = "Free Shipping";

        [BindProperty]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.EFT;

        // Hidden session-sourced fields 

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string City { get; set; } = string.Empty;

        [BindProperty]
        public string Province { get; set; } = string.Empty;

        [BindProperty]
        public string PostalCode { get; set; } = string.Empty;

        // GET
        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userId))
            {
                HttpContext.Session.SetString("ReturnUrl", "/Checkout");
                return RedirectToPage("/Account/Login", new { returnUrl = "/Checkout" });
            }

            CartItems = HttpContext.Session.GetCart();
            if (!CartItems.Any())
                return RedirectToPage("/Cart/Index");

            CartSubtotal = HttpContext.Session.GetCartTotal();
            ViewData["CartCount"] = HttpContext.Session.GetCartCount();

            Email = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? string.Empty;

            var userName = HttpContext.Session.GetString(SessionKeys.UserName) ?? string.Empty;
            if (!string.IsNullOrEmpty(userName))
            {
                var parts = userName.Split(' ', 2);
                FirstName = parts[0];
                LastName = parts.Length > 1 ? parts[1] : string.Empty;
            }

            return Page();
        }

        // POST 

        public async Task<IActionResult> OnPostPlaceOrderAsync()
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login", new { returnUrl = "/Checkout" });

            CartItems = HttpContext.Session.GetCart();
            CartSubtotal = HttpContext.Session.GetCartTotal();

            if (!CartItems.Any())
                return RedirectToPage("/Cart/Index");

            // Only strip hidden session fields — Phone is now visible and validated
            foreach (var key in new[] { "Email", "City", "Province", "PostalCode" })
                ModelState.Remove(key);

            if (!ModelState.IsValid)
                return Page();

            double shippingCost = ShippingMethod == "Express Shipping" ? 99.00 : 0.00;
            double orderTotal = CartSubtotal + shippingCost;

            var resolvedEmail = !string.IsNullOrEmpty(Email)
                ? Email
                : HttpContext.Session.GetString(SessionKeys.UserEmail) ?? string.Empty;

            // ── Build order items, fetching real product cost from Firestore ─
            var orderItems = new List<OrderItem>();
            foreach (var cartItem in CartItems)
            {
                // Fetch the live product to get its actual cost
                var product = await _firestoreService.GetProductByIdAsync(cartItem.ProductId);
                double snapshotCost = product?.Cost ?? 0.00;

                orderItems.Add(new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.ProductName,
                    ImageUrl = cartItem.ImageUrl,
                    SnapshotPrice = cartItem.Price,
                    SnapshotCost = snapshotCost,
                    Quantity = cartItem.Quantity,
                    SelectedSize = cartItem.SelectedSize ?? string.Empty
                });
            }

            var order = new Order
            {
                UserId = userId,
                CustomerName = $"{FirstName} {LastName}",
                CustomerEmail = resolvedEmail,
                CustomerPhone = Phone,
                DeliveryAddress = Address,
                OrderNotes = OrderNotes,
                ShippingMethod = ShippingMethod,
                OrderDate = Timestamp.FromDateTime(DateTime.UtcNow),
                Items = orderItems,
                TotalAmount = orderTotal,
                TotalCost = orderItems.Sum(i => i.SnapshotCost * i.Quantity),
                PaymentMethod = PaymentMethod,
                Status = OrderStatus.Paid
            };

            var orderId = await _firestoreService.CreateOrderAsync(order);

            HttpContext.Session.ClearCart();

            TempData["OrderId"] = orderId;

            return RedirectToPage("/Checkout/Success");
        }
    }
}