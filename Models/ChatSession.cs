using Google.Cloud.Firestore;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Models
{
    [FirestoreData]
    public class ChatSession
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("title")]
        public string Title { get; set; } = "New Chat";

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }

        [FirestoreProperty("messages")]
        public List<ChatMessage> Messages { get; set; } = new();
    }

    [FirestoreData]
    public class ChatMessage
    {
        [FirestoreProperty("role")]
        public string Role { get; set; } = string.Empty; // "user" or "ai"

        [FirestoreProperty("text")]
        public string Text { get; set; } = string.Empty;

        [FirestoreProperty("timestamp")]
        public Timestamp Timestamp { get; set; }
    }
}