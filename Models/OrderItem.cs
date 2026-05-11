using Google.Cloud.Firestore;
namespace INF4027W_BPTTIN002_MiniPrj_2026.Models
{
    [FirestoreData]
    public class OrderItem
    {
        [FirestoreProperty("productId")]
        public string ProductId { get; set; } = string.Empty;
        [FirestoreProperty("productName")]
        public string ProductName { get; set; } = string.Empty;
        [FirestoreProperty("quantity")]
        public int Quantity { get; set; }
        [FirestoreProperty("snapshotPrice")]
        public double SnapshotPrice { get; set; }
        [FirestoreProperty("snapshotCost")]
        public double SnapshotCost { get; set; }
        [FirestoreProperty("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;
        [FirestoreProperty("selectedSize")]
        public string SelectedSize { get; set; } = string.Empty;
    }
}