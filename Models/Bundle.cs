using Google.Cloud.Firestore;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Models
{
    [FirestoreData]
    public class Bundle
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("name")]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("occasion")]
        public string Occasion { get; set; } = string.Empty;

        [FirestoreProperty("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        [FirestoreProperty("itemIds")]
        public List<string> ItemIds { get; set; } = new();

        [FirestoreProperty("totalPrice")]
        public decimal TotalPrice { get; set; }

        [FirestoreProperty("explainableScores")]
        public Dictionary<string, double> ExplainableScores { get; set; } = new();

        [FirestoreProperty("riskFlags")]
        public List<string> RiskFlags { get; set; } = new();

        /// <summary>
        /// Fully resolved Product objects for rendering. Populated by BundleService,
        /// never persisted to Firestore. Must NEVER be null.
        /// </summary>
        public List<Product> ResolvedProducts { get; set; } = new();
    }
}