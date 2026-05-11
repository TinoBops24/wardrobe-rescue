using Google.Cloud.Firestore;
using Microsoft.Extensions.Caching.Memory;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using System.Text.RegularExpressions;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Services
{
    public class FirestoreService
    {
        // Dependencies

        private readonly FirestoreDb _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<FirestoreService> _logger;

        // Cache keys

        private const string ProductsCacheKey = "all_products";
        private const string BundlesCacheKey = "all_bundles";

        // Diagnostic counters for NFR reporting

        private static int _firestoreReadCount = 0;
        private static int _cacheHitCount = 0;

        public static int FirestoreReadCount => _firestoreReadCount;
        public static int CacheHitCount => _cacheHitCount;

        // Constructor

        public FirestoreService(
            FirestoreDb db,
            IMemoryCache cache,
            ILogger<FirestoreService> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        // ID Generators

        // prod_white-oxford-shirt
        private static string GenerateProductId(string name)
        {
            var slug = name.ToLower().Trim()
                           .Replace(" ", "-")
                           .Replace("'", "")
                           .Replace("&", "and");

            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
            slug = Regex.Replace(slug, @"-{2,}", "-");

            return $"prod_{slug}";
        }

        // bun_interview, bun_date-night
        private static string GenerateBundleId(string occasion)
        {
            var slug = occasion.ToLower().Trim()
                               .Replace(" ", "-")
                               .Replace("'", "");

            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
            slug = Regex.Replace(slug, @"-{2,}", "-");

            return $"bun_{slug}";
        }

        // order_20260218-001, order_20260218-002
        private async Task<string> GenerateOrderIdAsync()
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"order_{datePart}-";

            var snapshot = await _db.Collection("orders")
                .WhereGreaterThanOrEqualTo(FieldPath.DocumentId, prefix)
                .WhereLessThan(FieldPath.DocumentId, $"order_{datePart}.")
                .GetSnapshotAsync();

            var next = (snapshot.Count + 1).ToString("D3");
            return $"{prefix}{next}";
        }

        // chat_bpttin002_03021430
        private static string GenerateChatSessionId(string userId)
        {
            var prefix = userId.Contains("@") ? userId.Split("@")[0] : userId;
            var timestamp = DateTime.UtcNow.ToString("MMddHHmm");
            return $"chat_{prefix}_{timestamp}";
        }

        // Email address is the document ID for user profiles
        private static string GenerateUserId(string email)
            => email.ToLower().Trim();

        // Appends a counter if an ID already exists — prod_white-shirt-2
        private async Task<string> EnsureUniqueIdAsync(string collection, string baseId)
        {
            var doc = await _db.Collection(collection).Document(baseId).GetSnapshotAsync();
            if (!doc.Exists) return baseId;

            int counter = 2;
            while (true)
            {
                var candidateId = $"{baseId}-{counter}";
                var candidate = await _db.Collection(collection).Document(candidateId).GetSnapshotAsync();
                if (!candidate.Exists) return candidateId;
                counter++;
            }
        }

        // Cache Warming
        // Called once at startup so the first real user never pays the Firestore cost

        public async Task WarmCacheAsync()
        {
            _logger.LogInformation("Warming cache at startup...");
            await GetAllProductsAsync();
            await GetAllBundlesAsync();
            _logger.LogInformation("Cache warmed. Firestore reads so far: {Count}", _firestoreReadCount);
        }

        // Products

        public async Task<List<Product>> GetAllProductsAsync()
        {
            if (_cache.TryGetValue(ProductsCacheKey, out List<Product>? cached) && cached != null)
            {
                Interlocked.Increment(ref _cacheHitCount);
                _logger.LogDebug("Products served from cache. Cache hits: {Hits}", _cacheHitCount);
                return cached;
            }

            Interlocked.Increment(ref _firestoreReadCount);
            _logger.LogInformation("Firestore read #{Count} — fetching all products", _firestoreReadCount);

            var snapshot = await _db.Collection("products")
                .OrderBy("name")
                .GetSnapshotAsync();

            var products = snapshot.Documents
                .Select(d => d.ConvertTo<Product>())
                .ToList();

            _cache.Set(ProductsCacheKey, products);

            return products;
        }

        public async Task<Product?> GetProductByIdAsync(string id)
        {
            if (_cache.TryGetValue(ProductsCacheKey, out List<Product>? cached) && cached != null)
            {
                Interlocked.Increment(ref _cacheHitCount);
                return cached.FirstOrDefault(p => p.Id == id);
            }

            Interlocked.Increment(ref _firestoreReadCount);
            _logger.LogInformation("Firestore read #{Count} — single product {Id}", _firestoreReadCount, id);

            var doc = await _db.Collection("products").Document(id).GetSnapshotAsync();
            return doc.Exists ? doc.ConvertTo<Product>() : null;
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(string category)
        {
            var all = await GetAllProductsAsync();
            return all.Where(p => p.Category == category).ToList();
        }

        public async Task<List<Product>> GetPublishedProductsAsync()
        {
            var all = await GetAllProductsAsync();
            return all.Where(p => !p.IsDraft && !p.IsHiddenFromWeb).ToList();
        }

        public async Task<string> CreateProductAsync(Product product)
        {
            var baseId = GenerateProductId(product.Name);
            product.Id = await EnsureUniqueIdAsync("products", baseId);

            await _db.Collection("products").Document(product.Id).SetAsync(product);
            InvalidateProductCache();

            _logger.LogInformation("Product created: {Id}. Cache invalidated.", product.Id);
            return product.Id;
        }

        public async Task UpdateProductAsync(Product product)
        {
            await _db.Collection("products")
                .Document(product.Id)
                .SetAsync(product, SetOptions.MergeAll);
            InvalidateProductCache();
        }

        public async Task DeleteProductAsync(string id)
        {
            await _db.Collection("products").Document(id).DeleteAsync();
            InvalidateProductCache();
        }

        private void InvalidateProductCache()
        {
            _cache.Remove(ProductsCacheKey);
            _logger.LogInformation("Product cache invalidated — will repopulate on next request");
        }

        // Bundles

        public async Task<List<Bundle>> GetAllBundlesAsync()
        {
            if (_cache.TryGetValue(BundlesCacheKey, out List<Bundle>? cached) && cached != null)
            {
                Interlocked.Increment(ref _cacheHitCount);
                _logger.LogDebug("Bundles served from cache. Cache hits: {Hits}", _cacheHitCount);
                return cached;
            }

            Interlocked.Increment(ref _firestoreReadCount);
            _logger.LogInformation("Firestore read #{Count} — fetching all bundles", _firestoreReadCount);

            var snapshot = await _db.Collection("bundles")
                .OrderBy("name")
                .GetSnapshotAsync();

            var bundles = snapshot.Documents
                .Select(d => d.ConvertTo<Bundle>())
                .ToList();

            _cache.Set(BundlesCacheKey, bundles);

            return bundles;
        }

        public async Task<Bundle?> GetBundleByIdAsync(string id)
        {
            if (_cache.TryGetValue(BundlesCacheKey, out List<Bundle>? cached) && cached != null)
            {
                Interlocked.Increment(ref _cacheHitCount);
                return cached.FirstOrDefault(b => b.Id == id);
            }

            Interlocked.Increment(ref _firestoreReadCount);
            _logger.LogInformation("Firestore read #{Count} — single bundle {Id}", _firestoreReadCount, id);

            var doc = await _db.Collection("bundles").Document(id).GetSnapshotAsync();
            return doc.Exists ? doc.ConvertTo<Bundle>() : null;
        }

        public async Task<string> CreateBundleAsync(Bundle bundle)
        {
            var baseId = GenerateBundleId(bundle.Occasion);
            bundle.Id = await EnsureUniqueIdAsync("bundles", baseId);

            await _db.Collection("bundles").Document(bundle.Id).SetAsync(bundle);
            InvalidateBundleCache();

            _logger.LogInformation("Bundle created: {Id}. Cache invalidated.", bundle.Id);
            return bundle.Id;
        }

        public async Task UpdateBundleAsync(Bundle bundle)
        {
            await _db.Collection("bundles")
                .Document(bundle.Id)
                .SetAsync(bundle, SetOptions.MergeAll);
            InvalidateBundleCache();
        }

        public async Task DeleteBundleAsync(string id)
        {
            await _db.Collection("bundles").Document(id).DeleteAsync();
            InvalidateBundleCache();
        }

        private void InvalidateBundleCache()
        {
            _cache.Remove(BundlesCacheKey);
            _logger.LogInformation("Bundle cache invalidated — will repopulate on next request");
        }

        // Orders
        // Never cached — must always reflect the latest state

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            Interlocked.Increment(ref _firestoreReadCount);

            var snapshot = await _db.Collection("orders")
                .OrderByDescending("orderDate")
                .GetSnapshotAsync();

            return snapshot.Documents.Select(d => d.ConvertTo<Order>()).ToList();
        }

        public async Task<List<Order>> GetOrdersByUserIdAsync(string userId)
        {
            Interlocked.Increment(ref _firestoreReadCount);

            var snapshot = await _db.Collection("orders")
                .WhereEqualTo("userId", userId)
                .OrderByDescending("orderDate")
                .GetSnapshotAsync();

            return snapshot.Documents.Select(d => d.ConvertTo<Order>()).ToList();
        }

        public async Task<Order?> GetOrderByIdAsync(string id)
        {
            Interlocked.Increment(ref _firestoreReadCount);

            var doc = await _db.Collection("orders").Document(id).GetSnapshotAsync();
            return doc.Exists ? doc.ConvertTo<Order>() : null;
        }

        public async Task<string> CreateOrderAsync(Order order)
        {
            order.Id = await GenerateOrderIdAsync();

            await _db.Collection("orders").Document(order.Id).SetAsync(order);
            _logger.LogInformation("Order created: {Id}", order.Id);
            return order.Id;
        }

        // User Profiles
        // Never cached — security risk

        public async Task<UserProfile?> GetUserByFirebaseUidAsync(string firebaseUid)
        {
            Interlocked.Increment(ref _firestoreReadCount);

            var snapshot = await _db.Collection("userProfiles")
                .WhereEqualTo("firebaseUid", firebaseUid)
                .Limit(1)
                .GetSnapshotAsync();

            return snapshot.Documents.FirstOrDefault()?.ConvertTo<UserProfile>();
        }

        public async Task<UserProfile?> GetUserByIdAsync(string id)
        {
            Interlocked.Increment(ref _firestoreReadCount);

            var doc = await _db.Collection("userProfiles").Document(id).GetSnapshotAsync();
            return doc.Exists ? doc.ConvertTo<UserProfile>() : null;
        }

        public async Task<List<UserProfile>> GetAllUserProfilesAsync()
        {
            Interlocked.Increment(ref _firestoreReadCount);

            var snapshot = await _db.Collection("userProfiles").GetSnapshotAsync();
            return snapshot.Documents.Select(d => d.ConvertTo<UserProfile>()).ToList();
        }

        public async Task<string> CreateUserProfileAsync(UserProfile profile)
        {
            profile.Id = GenerateUserId(profile.Email);

            await _db.Collection("userProfiles").Document(profile.Id).SetAsync(profile);
            _logger.LogInformation("User profile created: {Id}", profile.Id);
            return profile.Id;
        }

        public async Task UpdateUserProfileAsync(UserProfile profile)
        {
            await _db.Collection("userProfiles")
                .Document(profile.Id)
                .SetAsync(profile, SetOptions.MergeAll);
        }

        // Remember Me Tokens
        // Never cached — security-critical, must always be fresh

        public async Task CreateRememberMeTokenAsync(RememberMeToken token)
        {
            await _db.Collection("rememberMeTokens")
                .Document(token.Id)
                .SetAsync(token);

            _logger.LogInformation(
                "Remember-me token created for user {UserId}. Expires: {Expiry}",
                token.UserId, token.ExpiresAt.ToDateTime());
        }

        public async Task<RememberMeToken?> GetRememberMeTokenAsync(string tokenHash)
        {
            var doc = await _db.Collection("rememberMeTokens")
                .Document(tokenHash)
                .GetSnapshotAsync();

            return doc.Exists ? doc.ConvertTo<RememberMeToken>() : null;
        }

        public async Task DeleteRememberMeTokenAsync(string tokenHash)
        {
            await _db.Collection("rememberMeTokens")
                .Document(tokenHash)
                .DeleteAsync();
        }

        // Cache metrics for admin performance page

        public static (int reads, int hits, double hitRate) GetCacheMetrics()
        {
            int total = _firestoreReadCount + _cacheHitCount;
            double rate = total > 0
                ? Math.Round((double)_cacheHitCount / total * 100, 1)
                : 0;

            return (_firestoreReadCount, _cacheHitCount, rate);
        }

        // Chat Sessions

        public async Task SaveChatSessionAsync(ChatSession session)
        {
            if (string.IsNullOrEmpty(session.Id))
                session.Id = GenerateChatSessionId(session.UserId);

            var docRef = _db.Collection("ChatSessions").Document(session.Id);
            session.UpdatedAt = Timestamp.GetCurrentTimestamp();
            await docRef.SetAsync(session);
        }

        public async Task<List<ChatSession>> GetUserChatSessionsAsync(string userId)
        {
            var snapshot = await _db.Collection("ChatSessions")
                .WhereEqualTo("userId", userId)
                .OrderByDescending("updatedAt")
                .GetSnapshotAsync();

            return snapshot.Documents.Select(d => d.ConvertTo<ChatSession>()).ToList();
        }

        public async Task<ChatSession?> GetChatSessionByIdAsync(string sessionId)
        {
            var doc = await _db.Collection("ChatSessions").Document(sessionId).GetSnapshotAsync();
            return doc.Exists ? doc.ConvertTo<ChatSession>() : null;
        }

        // Wishlist

        public async Task ToggleWishlistAsync(string userId, string productId)
        {
            var profile = await GetUserByIdAsync(userId);
            if (profile == null) return;

            if (profile.WishlistProductIds.Contains(productId))
                profile.WishlistProductIds.Remove(productId);
            else
                profile.WishlistProductIds.Add(productId);

            await _db.Collection("userProfiles")
                .Document(userId)
                .UpdateAsync("wishlist", profile.WishlistProductIds);

            _logger.LogInformation(
                "Wishlist toggled for user {UserId}, product {ProductId}", userId, productId);
        }
    }
}