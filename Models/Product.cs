using Google.Cloud.Firestore;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Models
{
    [FirestoreData]
    public class Product
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("name")]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("price")]
        public double Price { get; set; }

        [FirestoreProperty("cost")]
        public double Cost { get; set; }

        [FirestoreProperty("category")]
        public string Category { get; set; } = string.Empty;

        [FirestoreProperty("gender")]
        public string Gender { get; set; } = "Unisex";

        [FirestoreProperty("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        [FirestoreProperty("tags")]
        public List<string> Tags { get; set; } = new();

        [FirestoreProperty("sizes")]
        public List<string> Sizes { get; set; } = new();

        [FirestoreProperty("occasionTags")]
        public List<string> OccasionTags { get; set; } = new();

        [FirestoreProperty("dominantColor")]
        public string DominantColor { get; set; } = string.Empty;

        [FirestoreProperty("formalityScore")]
        public int FormalityScore { get; set; } = 3;

        [FirestoreProperty("stockLevel")]
        public int StockLevel { get; set; } = 999;

        [FirestoreProperty("isNew")]
        public bool IsNew { get; set; } = false;

        [FirestoreProperty("isDraft")]
        public bool IsDraft { get; set; } = true;

        [FirestoreProperty("isHiddenFromWeb")]
        public bool IsHiddenFromWeb { get; set; } = false;

        [FirestoreProperty("discountPercentage")]
        public int DiscountPercentage { get; set; } = 0;

        [FirestoreProperty("rating")]
        public double Rating { get; set; } = 5.0;

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        // Computed — not stored in Firestore
        public double? DiscountPrice => DiscountPercentage > 0
            ? Price * (1 - DiscountPercentage / 100.0)
            : null;
    }
}