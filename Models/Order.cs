using Google.Cloud.Firestore;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Models
{
    public enum PaymentMethod
    {
        Card,
        EFT,
        PayPal
    }

    public enum OrderStatus
    {
        Paid,
        Cancelled
    }

    [FirestoreData]
    public class Order
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [FirestoreProperty("customerEmail")]
        public string CustomerEmail { get; set; } = string.Empty;

        [FirestoreProperty("customerPhone")]
        public string CustomerPhone { get; set; } = string.Empty;

        [FirestoreProperty("deliveryAddress")]
        public string DeliveryAddress { get; set; } = string.Empty;

        [FirestoreProperty("orderNotes")]
        public string? OrderNotes { get; set; }

        [FirestoreProperty("shippingMethod")]
        public string ShippingMethod { get; set; } = "Free Shipping";

        [FirestoreProperty("orderDate")]
        public Timestamp OrderDate { get; set; }

        [FirestoreProperty("totalAmount")]
        public double TotalAmount { get; set; }

        [FirestoreProperty("totalCost")]
        public double TotalCost { get; set; }

        [FirestoreProperty("paymentMethod")]
        public PaymentMethod PaymentMethod { get; set; }

        [FirestoreProperty("status")]
        public OrderStatus Status { get; set; }

        [FirestoreProperty("items")]
        public List<OrderItem> Items { get; set; } = new();
    }
}