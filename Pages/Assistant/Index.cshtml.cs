using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;
using INF4027W_BPTTIN002_MiniPrj_2026.Helpers;
using Google.Cloud.Firestore;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Assistant
{
    public class IndexModel : PageModel
    {
        private readonly AiService _aiService;
        private readonly FirestoreService _firestoreService;
        private readonly BundleService _bundleService;

        private const string GuestSessionKey = "GuestChatSession";

        public IndexModel(
            AiService aiService,
            FirestoreService firestoreService,
            BundleService bundleService)
        {
            _aiService = aiService;
            _firestoreService = firestoreService;
            _bundleService = bundleService;
        }

        private static readonly string[] AllowedImageTypes =
            { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;
        private const int MaxPromptLength = 500;

        [BindProperty] public string UserPrompt { get; set; } = string.Empty;
        [BindProperty] public IFormFile? UploadedImage { get; set; }
        [BindProperty] public string? CurrentSessionId { get; set; }

        public string AssistantMessage { get; set; } = string.Empty;
        public bool HasResults { get; set; } = false;
        public List<Bundle> RecommendedBundles { get; set; } = new();
        public List<ScoredProduct> RecommendedProducts { get; set; } = new();
        public AssistantConstraints? LastConstraints { get; set; }
        public List<ChatSession> PastChats { get; set; } = new();
        public ChatSession? CurrentSession { get; set; }

        // GET
        public async Task<IActionResult> OnGetAsync(string? sessionId = null)
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);

            if (!string.IsNullOrEmpty(userId))
            {
                PastChats = await _firestoreService.GetUserChatSessionsAsync(userId);

                if (!string.IsNullOrEmpty(sessionId))
                {
                    CurrentSession = await _firestoreService.GetChatSessionByIdAsync(sessionId);
                    CurrentSessionId = sessionId;
                }
            }

            return Page();
        }

        // POST — Main conversation handler
        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);

            if (!string.IsNullOrEmpty(userId))
                PastChats = await _firestoreService.GetUserChatSessionsAsync(userId);

            if (string.IsNullOrEmpty(userId))
            {
                var guestJson = HttpContext.Session.GetString(GuestSessionKey);
                if (!string.IsNullOrEmpty(guestJson))
                    CurrentSession = JsonSerializer.Deserialize<ChatSession>(guestJson);
            }
            else if (!string.IsNullOrEmpty(CurrentSessionId))
            {
                CurrentSession = await _firestoreService.GetChatSessionByIdAsync(CurrentSessionId);
            }

            var hasText = !string.IsNullOrWhiteSpace(UserPrompt);
            var hasImage = UploadedImage != null;

            if (!hasText && !hasImage)
            {
                AssistantMessage = "Please describe your occasion or upload a clothing item to get started.";
                return Page();
            }

            if (hasText && UserPrompt.Length > MaxPromptLength)
                UserPrompt = UserPrompt[..MaxPromptLength];

            if (hasImage && (
                    !AllowedImageTypes.Contains(UploadedImage!.ContentType.ToLower()) ||
                    UploadedImage.Length > MaxImageSizeBytes))
            {
                AssistantMessage = "Please upload a valid image (JPG/PNG/WEBP) under 5 MB.";
                return Page();
            }

            AssistantConstraints constraints;

            if (hasImage)
            {
                using var ms = new MemoryStream();
                await UploadedImage!.CopyToAsync(ms);
                constraints = await _aiService.ExtractConstraintsFromImageAsync(
                    ms.ToArray(), UploadedImage.ContentType, hasText ? UserPrompt : null);
            }
            else
            {
                var history = CurrentSession?.Messages ?? new List<ChatMessage>();
                constraints = await _aiService.ExtractConstraintsAsync(UserPrompt, history);
            }

            if (!constraints.IsFashionRelated)
            {
                AssistantMessage = "I am Wardrobe Rescue's AI Stylist. I specialise in fashion " +
                                   "recommendations. Please ask me about outfits, specific clothing " +
                                   "items, or upload a photo of clothing.";
                HasResults = false;
                await SaveChatInteractionAsync(userId,
                    hasText ? UserPrompt : "[Image Uploaded]", AssistantMessage, null);
                return Page();
            }

            if (constraints.NeedsMoreInfo)
            {
                AssistantMessage = constraints.ClarifyingQuestion;
                HasResults = false;
                await SaveChatInteractionAsync(userId,
                    hasText ? UserPrompt : "[Image Uploaded]", AssistantMessage, null);
                return Page();
            }

            if (constraints.MaxBudget <= 0)
            {
                AssistantMessage = "Just one more thing — what's your budget? That way I can " +
                                   "make sure every piece I recommend is the right fit for you.";
                HasResults = false;
                await SaveChatInteractionAsync(userId,
                    hasText ? UserPrompt : "[Image Uploaded]", AssistantMessage, null);
                return Page();
            }

            constraints.TargetFormality = Math.Clamp(constraints.TargetFormality, 0, 5);
            constraints.MaxBudget = Math.Min(constraints.MaxBudget, 100_000m);

            if (string.IsNullOrWhiteSpace(constraints.OccasionContext))
                constraints.OccasionContext = hasText
                    ? UserPrompt[..Math.Min(UserPrompt.Length, 60)]
                    : "casual";

            var allProducts = (await _firestoreService.GetAllProductsAsync())
                .Where(p => !p.IsDraft && !p.IsHiddenFromWeb)
                .ToList();

            RecommendedBundles = _bundleService.GenerateBundles(allProducts, constraints);
            RecommendedProducts = RankProducts(allProducts, constraints);

            LastConstraints = constraints;
            HasResults = RecommendedBundles.Any() || RecommendedProducts.Any();
            AssistantMessage = BuildDynamicMessage(
                constraints, RecommendedBundles.Count, RecommendedProducts.Count);

            var recommendationSummary = BuildRecommendationSummary(RecommendedBundles, RecommendedProducts);

            await SaveChatInteractionAsync(userId,
                hasText ? UserPrompt : $"[Uploaded Image: {constraints.UploadedItemDescription}]",
                AssistantMessage,
                recommendationSummary);

            return Page();
        }

        // POST — Add Bundle to Cart (AJAX)
        // Returns JSON so the page does not redirect and the rendered recommendations remain visible.
        public async Task<IActionResult> OnPostAddBundleAsync(List<string> productIds, List<string> selectedSizes)
        {
            for (int i = 0; i < productIds.Count; i++)
            {
                var product = await _firestoreService.GetProductByIdAsync(productIds[i]);
                if (product != null)
                {
                    var size = selectedSizes != null && i < selectedSizes.Count
                        ? selectedSizes[i]
                        : string.Empty;
                    HttpContext.Session.AddToCart(product, 1, size);
                }
            }
            return new JsonResult(new
            {
                success = true,
                cartCount = HttpContext.Session.GetCartCount(),
                message = "Outfit added to your cart."
            });
        }

        // POST — Add Single Product to Cart (AJAX)
        // selectedSize is passed from the size picker modal before the item is added.
        public async Task<IActionResult> OnPostAddProductAsync(string productId, string? selectedSize)
        {
            var product = await _firestoreService.GetProductByIdAsync(productId);
            if (product == null)
                return new JsonResult(new { success = false, message = "Product not found." });

            HttpContext.Session.AddToCart(product, 1, selectedSize);

            return new JsonResult(new
            {
                success = true,
                cartCount = HttpContext.Session.GetCartCount(),
                message = $"{product.Name} added to your cart."
            });
        }

        // Chat Persistence
        private async Task SaveChatInteractionAsync(
            string? userId,
            string userText,
            string aiText,
            string? recommendationSummary)
        {
            // Append a structured summary of what was recommended to the stored AI message
            // so that future turns have full context about which specific items were shown.
            var storedAiText = string.IsNullOrEmpty(recommendationSummary)
                ? aiText
                : $"{aiText}\n\n[Recommended: {recommendationSummary}]";

            var timestamp = Timestamp.GetCurrentTimestamp();

            if (string.IsNullOrEmpty(userId))
            {
                CurrentSession ??= new ChatSession();
                CurrentSession.Messages.Add(new ChatMessage
                { Role = "user", Text = userText, Timestamp = timestamp });
                CurrentSession.Messages.Add(new ChatMessage
                { Role = "ai", Text = storedAiText, Timestamp = timestamp });

                HttpContext.Session.SetString(GuestSessionKey,
                    JsonSerializer.Serialize(CurrentSession));
                return;
            }

            ChatSession session;

            if (!string.IsNullOrEmpty(CurrentSessionId))
            {
                session = await _firestoreService.GetChatSessionByIdAsync(CurrentSessionId)
                          ?? new ChatSession { UserId = userId };
            }
            else
            {
                session = new ChatSession
                {
                    UserId = userId,
                    Title = userText.Length > 25 ? userText[..25] + "..." : userText,
                    UpdatedAt = timestamp
                };
            }

            session.Messages.Add(new ChatMessage
            { Role = "user", Text = userText, Timestamp = timestamp });
            session.Messages.Add(new ChatMessage
            { Role = "ai", Text = storedAiText, Timestamp = timestamp });

            await _firestoreService.SaveChatSessionAsync(session);

            CurrentSessionId = session.Id;
            CurrentSession = session;
            PastChats = await _firestoreService.GetUserChatSessionsAsync(userId);
        }

        private static string BuildRecommendationSummary(
            List<Bundle> bundles,
            List<ScoredProduct> products)
        {
            var parts = new List<string>();

            if (bundles.Any())
            {
                var bundleNames = bundles.Select(b =>
                    $"{b.Name} ({string.Join(", ", b.ResolvedProducts.Select(p => p.Name))})");
                parts.Add("Outfits: " + string.Join(" | ", bundleNames));
            }

            if (products.Any())
            {
                var productNames = products.Select(p => p.Product.Name);
                parts.Add("Individual pieces: " + string.Join(", ", productNames));
            }

            return string.Join(". ", parts);
        }

        // Dynamic Message Builder
        private static string BuildDynamicMessage(
            AssistantConstraints constraints,
            int bundleCount,
            int productCount)
        {
            if (bundleCount == 0 && productCount == 0)
            {
                if (constraints.HasImageInput && !string.IsNullOrEmpty(constraints.UploadedItemDescription))
                    return $"I love that **{constraints.UploadedItemDescription}** — " +
                           "unfortunately I couldn't find complementary pieces in your budget right now. " +
                           "Try increasing your budget or browse our full catalogue.";

                return "I couldn't find anything matching those exact criteria at the moment. " +
                       "Try broadening your budget or adjusting the occasion and I'll find something great for you.";
            }

            if (constraints.HasImageInput && !string.IsNullOrEmpty(constraints.UploadedItemDescription))
                return $"Great piece — that **{constraints.UploadedItemDescription}** has a lot of potential! " +
                       "Here are some items from our catalogue that would pair beautifully with it.";

            if (constraints.IsProductSearch && !string.IsNullOrEmpty(constraints.SearchCategory))
            {
                var item = !string.IsNullOrEmpty(constraints.AnchorItem)
                    ? constraints.AnchorItem
                    : constraints.SearchCategory.ToLower();
                return $"Here are the best **{item}** options I found within your budget — " +
                       "each one scored against your style and formality requirements.";
            }

            var occasion = !string.IsNullOrWhiteSpace(constraints.OccasionContext)
                ? constraints.OccasionContext.Trim()
                : "your occasion";

            var startsWithVowelSound = "aeiouAEIOU".Contains(occasion[0]);
            var article = startsWithVowelSound ? "an" : "a";

            if (bundleCount > 0 && productCount > 0)
                return $"For {article} **{occasion}**, I've put together {bundleCount} complete " +
                       $"{(bundleCount == 1 ? "look" : "looks")} and selected some individual pieces — " +
                       "mix and match to find what works best for you.";

            if (bundleCount > 0)
                return $"For {article} **{occasion}**, I've curated {bundleCount} complete " +
                       $"{(bundleCount == 1 ? "outfit" : "outfits")} that should have you stepping out in style.";

            return $"I've handpicked these pieces specifically for {article} **{occasion}** — " +
                   "each one scored against your budget and style requirements.";
        }

        // Deterministic Product Ranking Engine
        private static List<ScoredProduct> RankProducts(
            List<Product> products,
            AssistantConstraints constraints)
        {
            var scored = new List<ScoredProduct>();

            foreach (var product in products)
            {
                // Hard gender filter — Women and Unisex pass for a Women query
                if (!string.IsNullOrEmpty(constraints.Gender)
                    && !string.Equals(product.Gender, constraints.Gender, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(product.Gender, "Unisex", StringComparison.OrdinalIgnoreCase))
                    continue;

                double score = 0;
                var reasons = new List<string>();

                // Gender specificity bonus — a product explicitly tagged for the requested
                // gender ranks above an equivalent Unisex product so women's items
                // surface before unisex ones for a Women query and vice versa.
                if (!string.IsNullOrEmpty(constraints.Gender) &&
                    string.Equals(product.Gender, constraints.Gender, StringComparison.OrdinalIgnoreCase))
                {
                    score += 15;
                    reasons.Add($"Designed specifically for {constraints.Gender}.");
                }

                // Budget scoring
                var price = (decimal)(product.DiscountPrice ?? product.Price);
                if (price <= constraints.MaxBudget)
                {
                    score += 30;
                    reasons.Add($"Comfortably within your R{constraints.MaxBudget:N0} budget.");
                }
                else if (price <= constraints.MaxBudget * 1.15m)
                {
                    score += 15;
                    reasons.Add("Slightly over budget, but a high style match.");
                }
                else
                {
                    continue;
                }

                if (constraints.IsProductSearch)
                {
                    if (string.IsNullOrEmpty(constraints.SearchCategory))
                        continue;

                    if (!string.Equals(product.Category, constraints.SearchCategory,
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    score += 50;
                    reasons.Add($"Matches your request for {constraints.SearchCategory}.");
                }
                else
                {
                    score += 20;
                }

                // Formality scoring
                if (constraints.TargetFormality > 0)
                {
                    var diff = Math.Abs(product.FormalityScore - constraints.TargetFormality);
                    if (diff == 0)
                    {
                        score += 20;
                        reasons.Add("Perfect formality level for this occasion.");
                    }
                    else if (diff == 1)
                    {
                        score += 10;
                        reasons.Add("Versatile formality — easily styled up or down.");
                    }
                }

                if (score > 0)
                {
                    scored.Add(new ScoredProduct
                    {
                        Product = product,
                        Score = Math.Min(100, score),
                        Reasons = reasons
                    });
                }
            }

            return scored
                .OrderByDescending(s => s.Score)
                .Take(constraints.IsProductSearch ? 6 : 3)
                .ToList();
        }
    }

    public class ScoredProduct
    {
        public Product Product { get; set; } = null!;
        public double Score { get; set; }
        public List<string> Reasons { get; set; } = new();
    }
}