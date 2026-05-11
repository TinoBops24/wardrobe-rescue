using Google.Cloud.Firestore;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Models
{
    /// <summary>
    /// Persistent login token stored in Firestore.
    /// One document per device/browser — multiple tokens per user are valid.
    /// Document ID = the token hash (SHA-256 of the raw token sent in the cookie).
    /// </summary>
    [FirestoreData]
    public class RememberMeToken
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("userEmail")]
        public string UserEmail { get; set; } = string.Empty;

        [FirestoreProperty("role")]
        public string Role { get; set; } = string.Empty;

        [FirestoreProperty("expiresAt")]
        public Timestamp ExpiresAt { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        /// <summary>
        /// Browser User-Agent — informational only, not used for security.
        /// Useful for future "active sessions" page.
        /// </summary>
        [FirestoreProperty("deviceHint")]
        public string DeviceHint { get; set; } = string.Empty;
    }
}