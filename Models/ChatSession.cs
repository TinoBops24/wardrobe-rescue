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

        /// <summary>
        /// Serialised AssistantConstraints for an AI turn that produced recommendations,
        /// null for every other message. The engine is deterministic, so this rebuilds
        /// that turn's cards on any later load — which is what keeps older
        /// recommendations on screen instead of only the newest. JSON because
        /// AssistantConstraints is a plain POCO, not [FirestoreData].
        /// ponytail: recomputed, not snapshotted — a catalogue edit can change what an
        /// old bubble shows. Persist the resolved product IDs if that ever matters.
        /// </summary>
        [FirestoreProperty("constraints")]
        public string? ConstraintsJson { get; set; }
    }
}