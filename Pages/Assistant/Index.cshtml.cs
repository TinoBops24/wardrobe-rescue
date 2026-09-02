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
        public List<ChatSession> PastChats { get; set; } = new();
        public ChatSession? CurrentSession { get; set; }

        /// <summary>
        /// Recommendations keyed by their position in CurrentSession.Messages, so every
        /// turn that produced cards renders its own set and earlier ones stay on screen.
        /// </summary>
        public Dictionary<int, TurnResults> ResultsByMessage { get; set; } = new();

        // GET
        public async Task<IActionResult> OnGetAsync(string? sessionId = null, bool reset = false)
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);

            if (reset)
            {
                HttpContext.Session.Remove(GuestSessionKey);
                return RedirectToPage();
            }

            if (!string.IsNullOrEmpty(userId))
            {
                PastChats = await _firestoreService.GetUserChatSessionsAsync(userId);

                if (!string.IsNullOrEmpty(sessionId))
                {
                    CurrentSession = await _firestoreService.GetChatSessionByIdAsync(sessionId);
                    CurrentSessionId = sessionId;
                }
            }
            else
            {
                var guestJson = HttpContext.Session.GetString(GuestSessionKey);
                if (!string.IsNullOrEmpty(guestJson))
                    CurrentSession = JsonSerializer.Deserialize<ChatSession>(guestJson);
            }

            await BuildResultsAsync();

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
                return RedirectToPage(new { sessionId = CurrentSessionId });
            }

            if (hasText && UserPrompt.Length > MaxPromptLength)
                UserPrompt = UserPrompt[..MaxPromptLength];

            if (hasImage && (
                    !AllowedImageTypes.Contains(UploadedImage!.ContentType.ToLower()) ||
                    UploadedImage.Length > MaxImageSizeBytes))
            {
                AssistantMessage = "Please upload a valid image (JPG/PNG/WEBP) under 5 MB.";
                return RedirectToPage(new { sessionId = CurrentSessionId });
            }

            AssistantConstraints constraints;
            var history = CurrentSession?.Messages ?? new List<ChatMessage>();

            if (hasImage)
            {
                using var ms = new MemoryStream();
                await UploadedImage!.CopyToAsync(ms);
                constraints = await _aiService.ExtractConstraintsFromImageAsync(
                    ms.ToArray(), UploadedImage.ContentType, hasText ? UserPrompt : null, history);
            }
            else
            {
                constraints = await _aiService.ExtractConstraintsAsync(UserPrompt, history);
            }

            // Turns that need words, not a product rail: a question to answer, or an
            // off-topic prompt to decline. Same composer, empty-handed briefing.
            if (constraints.AnswerDirectly || !constraints.IsFashionRelated)
            {
                var brief = constraints.IsFashionRelated
                    ? "[BRIEFING - not from the customer]\nThe customer asked a question rather than for products. Nothing was retrieved, so recommend nothing specific. Answer them."
                    : "[BRIEFING - not from the customer]\nOff-topic for a clothing store. Decline in one warm sentence and offer to help with their wardrobe instead.";

                var text = hasText ? UserPrompt : "[Image Uploaded]";

                AssistantMessage = await _aiService.ComposeReplyAsync(
                                       history.Append(new ChatMessage { Role = "user", Text = text }).ToList(),
                                       constraints,
                                       brief)
                                   ?? "I'm your Wardrobe Rescue stylist — ask me about outfits, "
                                      + "specific pieces, or upload a photo of something you own.";

                await SaveChatInteractionAsync(userId, text, AssistantMessage, null, null);
                return RedirectToPage(new { sessionId = CurrentSessionId });
            }

            if (constraints.NeedsMoreInfo)
            {
                AssistantMessage = constraints.ClarifyingQuestion;
                await SaveChatInteractionAsync(userId,
                    hasText ? UserPrompt : "[Image Uploaded]", AssistantMessage, null, null);
                return RedirectToPage(new { sessionId = CurrentSessionId });
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

            var bundles = await CurateOutfitsAsync(allProducts, constraints);
            var products = RankProducts(allProducts, constraints);

            var userText = hasText
                ? UserPrompt
                : $"[Uploaded Image: {constraints.UploadedItemDescription}]";

            // The engine has decided what may be shown; the model decides how to say it,
            // grounded in that exact list. Templates remain the fallback when the call fails.
            AssistantMessage = await _aiService.ComposeReplyAsync(
                                   history.Append(new ChatMessage { Role = "user", Text = userText }).ToList(),
                                   constraints,
                                   BuildRetrievalBriefing(constraints, bundles, products))
                               ?? BuildDynamicMessage(constraints, bundles.Count, products.Count);

            // The constraints ride along on the stored AI message so this turn's cards can
            // be rebuilt on every later page load, alongside every earlier turn's.
            await SaveChatInteractionAsync(
                userId,
                userText,
                AssistantMessage,
                BuildRecommendationSummary(bundles, products),
                bundles.Any() || products.Any() ? constraints : null,
                bundles);

            return RedirectToPage(new { sessionId = CurrentSessionId });
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

        /// <summary>
        /// Shortlist, curate, validate, fall back. The model picks from products the
        /// engine already approved, and only looks that survive validation and scoring
        /// are shown. An empty or unusable result drops through to the recipes, so a bad
        /// AI turn costs coherence, never the recommendation itself.
        /// </summary>
        private async Task<List<Bundle>> CurateOutfitsAsync(
            List<Product> allProducts,
            AssistantConstraints constraints)
        {
            var shortlist = _bundleService.BuildShortlist(allProducts, constraints);
            var proposals = await _aiService.CurateOutfitsAsync(shortlist, constraints);
            var curated = _bundleService.ValidateProposals(proposals, shortlist, constraints);

            if (curated.Count > 0)
                return curated;

            return _bundleService.GenerateBundles(allProducts, constraints);
        }

        // Post/Redirect/Get support.
        // OnPostAsync ends in a redirect so this page's history entry is a GET.
        // Without it, going back here after adding to cart replays the POST, which
        // re-runs the AI call and appends the same pair of chat messages again.
        // The GET rebuilds every turn's cards from the constraints stored on each
        // message, deterministically and with no second AI call.
        private async Task BuildResultsAsync()
        {
            var messages = CurrentSession?.Messages;
            if (messages == null)
                return;

            var turns = messages
                .Select((message, index) => (message, index))
                .Where(t => t.message.Role == "ai" && !string.IsNullOrEmpty(t.message.ConstraintsJson))
                .ToList();

            if (turns.Count == 0)
                return;

            var allProducts = (await _firestoreService.GetAllProductsAsync())
                .Where(p => !p.IsDraft && !p.IsHiddenFromWeb)
                .ToList();

            foreach (var (message, index) in turns)
            {
                AssistantConstraints? constraints;
                List<OutfitProposal>? shown = null;
                try
                {
                    constraints = JsonSerializer.Deserialize<AssistantConstraints>(message.ConstraintsJson!);

                    if (!string.IsNullOrEmpty(message.OutfitsJson))
                        shown = JsonSerializer.Deserialize<List<OutfitProposal>>(message.OutfitsJson);
                }
                catch (JsonException)
                {
                    continue; // A turn we can no longer rebuild renders as text only.
                }

                if (constraints == null)
                    continue;

                // Replay the outfits this turn showed. Turns saved before outfits were
                // stored fall back to the recipes, which is what they showed anyway.
                var bundles = shown == null
                    ? _bundleService.GenerateBundles(allProducts, constraints)
                    : _bundleService.ValidateProposals(
                        shown, _bundleService.BuildShortlist(allProducts, constraints), constraints);

                ResultsByMessage[index] = new TurnResults(
                    bundles,
                    RankProducts(allProducts, constraints),
                    constraints.IsProductSearch);
            }
        }

        // Chat Persistence
        private async Task SaveChatInteractionAsync(
            string? userId,
            string userText,
            string aiText,
            string? recommendationSummary,
            AssistantConstraints? constraints,
            List<Bundle>? bundles = null)
        {
            // Append a structured summary of what was recommended to the stored AI message
            // so that future turns have full context about which specific items were shown.
            var storedAiText = string.IsNullOrEmpty(recommendationSummary)
                ? aiText
                : $"{aiText}\n\n[Recommended: {recommendationSummary}]";

            var timestamp = Timestamp.GetCurrentTimestamp();

            var turn = new[]
            {
                new ChatMessage { Role = "user", Text = userText, Timestamp = timestamp },
                new ChatMessage
                {
                    Role = "ai",
                    Text = storedAiText,
                    Timestamp = timestamp,
                    ConstraintsJson = constraints == null
                        ? null
                        : JsonSerializer.Serialize(constraints),
                    OutfitsJson = bundles == null || bundles.Count == 0
                        ? null
                        : JsonSerializer.Serialize(bundles.Select(b =>
                            new OutfitProposal { Name = b.Name, ProductIds = b.ItemIds }))
                }
            };

            if (string.IsNullOrEmpty(userId))
            {
                CurrentSession ??= new ChatSession();
                CurrentSession.Messages.AddRange(turn);

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

            session.Messages.AddRange(turn);

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

        /// <summary>
        /// The grounding payload for the reply composer: what was asked for and exactly
        /// what the engine retrieved. Data only - what to DO with it is the composer's
        /// system prompt. Anything absent here is something the model may not claim.
        /// </summary>
        private static string BuildRetrievalBriefing(
            AssistantConstraints constraints,
            List<Bundle> bundles,
            List<ScoredProduct> products)
        {
            var outfits = bundles.Select(b =>
                $"- \"{b.Name}\" (R{b.TotalPrice:N0} total): {string.Join(", ", b.ResolvedProducts.Select(p => p.Name))}"
                + string.Concat(b.RiskFlags.Select(f => $"\n    note: {f}")));

            var pieces = products.Select(p =>
                $"- {p.Product.Name} ({p.Product.Category}, {p.Product.DominantColor}, R{p.Product.DiscountPrice ?? p.Product.Price:N0})");

            return $"""
                [BRIEFING - not from the customer]
                Shopping for: {(string.IsNullOrEmpty(constraints.Gender) ? "unspecified" : constraints.Gender)}
                Occasion: {constraints.OccasionContext}
                Budget: {(constraints.MaxBudget > 0 ? $"R{constraints.MaxBudget:N0}" : "not stated - ask only if it is actually relevant")}
                Wants: {(constraints.IsProductSearch ? $"a specific item ({constraints.AnchorItem})" : "a complete outfit")}
                Uploaded: {(constraints.HasImageInput ? constraints.UploadedItemDescription : "nothing")}

                COMPLETE OUTFITS RETRIEVED:
                {(outfits.Any() ? string.Join("\n", outfits) : "none")}

                INDIVIDUAL PIECES RETRIEVED:
                {(pieces.Any() ? string.Join("\n", pieces) : "none")}
                """;
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

                // Budget scoring. No budget stated means no price cap, so nothing is
                // filtered out on price and every product scores as if it fits.
                var price = (decimal)(product.DiscountPrice ?? product.Price);
                if (constraints.MaxBudget <= 0)
                {
                    score += 30;
                }
                else if (price <= constraints.MaxBudget)
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

    /// <summary>
    /// One chat turn's recommendations, rendered under that turn's message.
    /// IsProductSearch decides the order: pieces lead a product search, outfits lead a brief.
    /// </summary>
    public record TurnResults(List<Bundle> Bundles, List<ScoredProduct> Products, bool IsProductSearch);

    public class ScoredProduct
    {
        public Product Product { get; set; } = null!;
        public double Score { get; set; }
        public List<string> Reasons { get; set; } = new();
    }
}