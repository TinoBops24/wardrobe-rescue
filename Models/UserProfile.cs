using Google.Cloud.Firestore;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Models
{
    [FirestoreData]
    public class UserProfile
    {
        [FirestoreDocumentId]
        public string Id { get; set; } 

        [FirestoreProperty("firebaseUid")]
        public string FirebaseUid { get; set; } // Link to Firebase Auth

        [FirestoreProperty("email")]
        public string Email { get; set; }

        [FirestoreProperty("firstName")]
        public string FirstName { get; set; }

        [FirestoreProperty("lastName")]
        public string LastName { get; set; }

        [FirestoreProperty("role")]
        public string Role { get; set; } = "Customer"; // "Admin" or "Customer"

        [FirestoreProperty("wishlist")]
        public List<string> WishlistProductIds { get; set; } = new();

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("isActive")]
        public bool IsActive { get; set; } = true;
    }
}