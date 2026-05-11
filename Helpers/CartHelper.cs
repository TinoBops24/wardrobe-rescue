using System.Text.Json;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using Microsoft.AspNetCore.Http;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Helpers
{
    public static class CartHelper
    {
        public static List<CartItem> GetCart(this ISession session)
        {
            var json = session.GetString(SessionKeys.Cart);
            if (string.IsNullOrEmpty(json))
                return new List<CartItem>();
            return JsonSerializer.Deserialize<List<CartItem>>(json)
                   ?? new List<CartItem>();
        }

        private static void SaveCart(this ISession session, List<CartItem> cart)
        {
            session.SetString(SessionKeys.Cart, JsonSerializer.Serialize(cart));
        }

        // Original overload — preserved for all existing callers (bundle add, cart page, etc.)
        public static void AddToCart(this ISession session, Product product, int quantity = 1)
            => session.AddToCart(product, quantity, null);

        // Size-aware overload used by the AI Stylist single-product add.
        // Treats the same product in different sizes as separate cart line items.
        public static void AddToCart(this ISession session, Product product, int quantity, string? selectedSize)
        {
            var cart = session.GetCart();
            var existing = cart.FirstOrDefault(c =>
                c.ProductId == product.Id &&
                c.SelectedSize == selectedSize);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.DiscountPrice ?? product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl,
                    SelectedSize = selectedSize
                });
            }

            session.SaveCart(cart);
        }

        public static void RemoveFromCart(this ISession session, string productId)
        {
            var cart = session.GetCart();
            cart.RemoveAll(c => c.ProductId == productId);
            session.SaveCart(cart);
        }

        public static void UpdateQuantity(this ISession session, string productId, int quantity)
        {
            var cart = session.GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item == null) return;
            if (quantity <= 0)
                cart.Remove(item);
            else
                item.Quantity = quantity;
            session.SaveCart(cart);
        }

        public static int GetCartCount(this ISession session)
            => session.GetCart().Sum(c => c.Quantity);

        public static double GetCartTotal(this ISession session)
            => session.GetCart().Sum(c => c.Price * c.Quantity);

        public static void ClearCart(this ISession session)
            => session.Remove(SessionKeys.Cart);
    }
}