using INF4027W_BPTTIN002_MiniPrj_2026.Models;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Services
{
    public class BundleService
    {
        private static readonly List<BundleDefinition> Definitions = new()
        {
            // Women — Interview
            new BundleDefinition
            {
                Name = "Interview Authority",
                Gender = "Women",
                OccasionTag = "Interview",
                Description = "A polished, commanding look for your next interview.",
                Rules = new()
                {
                    new BundleRule { Category = "Jackets",     MinFormality = 4, MaxFormality = 5 },
                    new BundleRule { Category = "Bottoms",     MinFormality = 4, MaxFormality = 5 },
                    new BundleRule { Category = "Tops",        MinFormality = 4, MaxFormality = 5 },
                    new BundleRule { Category = "Shoes",       MinFormality = 4, MaxFormality = 5 },
                    new BundleRule { Category = "Accessories", MinFormality = 3, MaxFormality = 5, IsOptional = true }
                }
            },

            // Women — Tech Interview
            new BundleDefinition
            {
                Name = "Tech Ready",
                Gender = "Women",
                OccasionTag = "Tech Interview",
                Description = "Smart casual confidence for a tech environment.",
                Rules = new()
                {
                    new BundleRule { Category = "Tops",        MinFormality = 3, MaxFormality = 4 },
                    new BundleRule { Category = "Bottoms",     MinFormality = 3, MaxFormality = 4 },
                    new BundleRule { Category = "Shoes",       MinFormality = 3, MaxFormality = 4 },
                    new BundleRule { Category = "Jackets",     MinFormality = 3, MaxFormality = 4, IsOptional = true },
                    new BundleRule { Category = "Accessories", MinFormality = 2, MaxFormality = 4, IsOptional = true }
                }
            },

            // Women — Smart Casual
            new BundleDefinition
            {
                Name = "Smart Office",
                Gender = "Women",
                OccasionTag = "Smart Casual",
                Description = "Effortlessly professional for the modern workplace.",
                Rules = new()
                {
                    new BundleRule { Category = "Tops",        MinFormality = 3, MaxFormality = 5 },
                    new BundleRule { Category = "Bottoms",     MinFormality = 3, MaxFormality = 5 },
                    new BundleRule { Category = "Shoes",       MinFormality = 3, MaxFormality = 5 },
                    new BundleRule { Category = "Accessories", MinFormality = 3, MaxFormality = 5, IsOptional = true }
                }
            },

            // Women — Date Night
            new BundleDefinition
            {
                Name = "Date Night Minimalist",
                Gender = "Women",
                OccasionTag = "Date Night",
                Description = "Simple, elegant, and unforgettable.",
                Rules = new()
                {
                    new BundleRule { Category = "Dresses",     MinFormality = 2, MaxFormality = 4 },
                    new BundleRule { Category = "Shoes",       MinFormality = 2, MaxFormality = 4 },
                    new BundleRule { Category = "Accessories", MinFormality = 2, MaxFormality = 4, IsOptional = true }
                }
            },

            // Women — Graduation
            new BundleDefinition
            {
                Name = "Graduation Elegance",
                Gender = "Women",
                OccasionTag = "Graduation",
                Description = "Celebrate your achievement in style.",
                Rules = new()
                {
                    new BundleRule { Category = "Dresses",     MinFormality = 4, MaxFormality = 5 },
                    new BundleRule { Category = "Shoes",       MinFormality = 4, MaxFormality = 5 },
                    new BundleRule { Category = "Accessories", MinFormality = 3, MaxFormality = 5, IsOptional = true },
                    new BundleRule { Category = "Jackets",     MinFormality = 4, MaxFormality = 5, IsOptional = true }
                }
            },

            // Women — Summer
            new BundleDefinition
            {
                Name = "Cape Town Summer",
                Gender = "Women",
                OccasionTag = "Summer",
                Description = "Light, breezy, and ready for the Cape Town heat.",
                Rules = new()
                {
                    new BundleRule { Category = "Tops",        MinFormality = 1, MaxFormality = 2 },
                    new BundleRule { Category = "Bottoms",     MinFormality = 1, MaxFormality = 2 },
                    new BundleRule { Category = "Shoes",       MinFormality = 1, MaxFormality = 2 },
                    new BundleRule { Category = "Dresses",     MinFormality = 1, MaxFormality = 3, IsOptional = true },
                    new BundleRule { Category = "Accessories", MinFormality = 1, MaxFormality = 3, IsOptional = true }
                }
            },

            // Women — Weekend
            new BundleDefinition
            {
                Name = "Weekend Flex",
                Gender = "Women",
                OccasionTag = "Weekend",
                Description = "Effortless streetwear energy for your days off.",
                Rules = new()
                {
                    new BundleRule { Category = "Bottoms",   MinFormality = 1, MaxFormality = 2 },
                    new BundleRule { Category = "Tops",      MinFormality = 1, MaxFormality = 2 },
                    new BundleRule { Category = "Shoes",     MinFormality = 1, MaxFormality = 2 },
                    new BundleRule { Category = "Outerwear", MinFormality = 1, MaxFormality = 3, IsOptional = true }
                }
            },

            // Women — Casual
            new BundleDefinition
            {
                Name = "Elevated After-Work",
                Gender = "Women",
                OccasionTag = "Casual",
                Description = "Relaxed but put-together for after-hours.",
                Rules = new()
                {
                    new BundleRule { Category = "Bottoms",     MinFormality = 2, MaxFormality = 3 },
                    new BundleRule { Category = "Tops",        MinFormality = 2, MaxFormality = 3 },
                    new BundleRule { Category = "Shoes",       MinFormality = 1, MaxFormality = 3 },
                    new BundleRule { Category = "Accessories", MinFormality = 2, MaxFormality = 4, IsOptional = true }
                }
            },

            // Men — Interview
            new BundleDefinition
            {
                Name = "Corporate Interview",
                Gender = "Men",
                OccasionTag = "Interview",
                Description = "Confidence starts with the right outfit.",
                Rules = new()
                {
                    new BundleRule { Category = "Jackets",     MinFormality = 4, MaxFormality = 5 },
                    new BundleRule { Category = "Bottoms",     MinFormality = 4, MaxFormality = 5 },
                    new BundleRule { Category = "Tops",        MinFormality = 4, MaxFormality = 5 },
                    new BundleRule { Category = "Shoes",       MinFormality = 4, MaxFormality = 5 },
                    new BundleRule { Category = "Accessories", MinFormality = 3, MaxFormality = 5, IsOptional = true }
                }
            },

            // Men — Tech Interview
            new BundleDefinition
            {
                Name = "Tech Interview Edit",
                Gender = "Men",
                OccasionTag = "Tech Interview",
                Description = "Smart casual authority — no tie required.",
                Rules = new()
                {
                    new BundleRule { Category = "Tops",      MinFormality = 3, MaxFormality = 4 },
                    new BundleRule { Category = "Bottoms",   MinFormality = 3, MaxFormality = 4 },
                    new BundleRule { Category = "Shoes",     MinFormality = 3, MaxFormality = 4 },
                    new BundleRule { Category = "Jackets",   MinFormality = 3, MaxFormality = 4, IsOptional = true },
                    new BundleRule { Category = "Outerwear", MinFormality = 2, MaxFormality = 4, IsOptional = true }
                }
            },

            // Men — Smart Casual
            new BundleDefinition
            {
                Name = "Smart Casual Authority",
                Gender = "Men",
                OccasionTag = "Smart Casual",
                Description = "Sharp without trying too hard.",
                Rules = new()
                {
                    new BundleRule { Category = "Bottoms",   MinFormality = 2, MaxFormality = 3 },
                    new BundleRule { Category = "Outerwear", MinFormality = 2, MaxFormality = 4, IsOptional = true },
                    new BundleRule { Category = "Tops",      MinFormality = 1, MaxFormality = 3 },
                    new BundleRule { Category = "Shoes",     MinFormality = 1, MaxFormality = 3 }
                }
            },

            // Men — Date Night
            new BundleDefinition
            {
                Name = "Modern Evening",
                Gender = "Men",
                OccasionTag = "Date Night",
                Description = "Sleek and intentional for the evening ahead.",
                Rules = new()
                {
                    new BundleRule { Category = "Bottoms", MinFormality = 3, MaxFormality = 4 },
                    new BundleRule { Category = "Tops",    MinFormality = 3, MaxFormality = 4 },
                    new BundleRule { Category = "Shoes",   MinFormality = 3, MaxFormality = 5 },
                    new BundleRule { Category = "Jackets", MinFormality = 3, MaxFormality = 4, IsOptional = true }
                }
            },

            // Men — Graduation
            new BundleDefinition
            {
                Name = "Graduation Sharp",
                Gender = "Men",
                OccasionTag = "Graduation",
                Description = "Mark the milestone looking your best.",
                Rules = new()
                {
                    new BundleRule { Category = "Jackets",     MinFormality = 4, MaxFormality = 5 },
                    new BundleRule { Category = "Bottoms",     MinFormality = 4, MaxFormality = 5 },
                    new BundleRule { Category = "Tops",        MinFormality = 4, MaxFormality = 5 },
                    new BundleRule { Category = "Shoes",       MinFormality = 4, MaxFormality = 5 },
                    new BundleRule { Category = "Accessories", MinFormality = 3, MaxFormality = 5, IsOptional = true }
                }
            },

            // Men — Summer
            new BundleDefinition
            {
                Name = "Summer Ease",
                Gender = "Men",
                OccasionTag = "Summer",
                Description = "Cool and considered for warm Cape Town days.",
                Rules = new()
                {
                    new BundleRule { Category = "Tops",      MinFormality = 1, MaxFormality = 2 },
                    new BundleRule { Category = "Bottoms",   MinFormality = 1, MaxFormality = 2 },
                    new BundleRule { Category = "Shoes",     MinFormality = 1, MaxFormality = 2 },
                    new BundleRule { Category = "Outerwear", MinFormality = 1, MaxFormality = 2, IsOptional = true }
                }
            },

            // Men — Weekend
            new BundleDefinition
            {
                Name = "Weekend Streetwear",
                Gender = "Men",
                OccasionTag = "Weekend",
                Description = "Relaxed fits with a streetwear edge.",
                Rules = new()
                {
                    new BundleRule { Category = "Bottoms",   MinFormality = 1, MaxFormality = 2 },
                    new BundleRule { Category = "Tops",      MinFormality = 1, MaxFormality = 2 },
                    new BundleRule { Category = "Shoes",     MinFormality = 1, MaxFormality = 2 },
                    new BundleRule { Category = "Outerwear", MinFormality = 1, MaxFormality = 3, IsOptional = true }
                }
            },

            // Men — Casual
            new BundleDefinition
            {
                Name = "Relaxed Professional",
                Gender = "Men",
                OccasionTag = "Casual",
                Description = "Laid-back but never sloppy.",
                Rules = new()
                {
                    new BundleRule { Category = "Bottoms", MinFormality = 2, MaxFormality = 3 },
                    new BundleRule { Category = "Jackets", MinFormality = 2, MaxFormality = 4, IsOptional = true },
                    new BundleRule { Category = "Tops",    MinFormality = 1, MaxFormality = 2 },
                    new BundleRule { Category = "Shoes",   MinFormality = 1, MaxFormality = 3 }
                }
            }
        };

        public List<Bundle> GenerateBundles(List<Product> products, AssistantConstraints constraints)
        {
            var requestedOccasion = constraints.OccasionContext?.Trim() ?? string.Empty;

            var pool = products
                .Where(p =>
                    string.Equals(p.Gender, constraints.Gender, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.Gender, "Unisex", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var results = new List<Bundle>();

            foreach (var definition in Definitions)
            {
                if (!string.Equals(definition.Gender, constraints.Gender, StringComparison.OrdinalIgnoreCase))
                    continue;

                var resolvedProducts = new List<Product>();
                var usedIds = new HashSet<string>();
                var missingCategories = new List<string>();

                // A required slot tries the rule's formality band, then one step
                // either side of it, before it counts as a gap.
                Product? Pick(BundleRule rule, int tolerance) => pool
                    .Where(p =>
                        string.Equals(p.Category, rule.Category, StringComparison.OrdinalIgnoreCase) &&
                        p.FormalityScore >= rule.MinFormality - tolerance &&
                        p.FormalityScore <= rule.MaxFormality + tolerance &&
                        !usedIds.Contains(p.Id))
                    .OrderByDescending(p =>
                        string.Equals(p.Gender, constraints.Gender, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                    .ThenByDescending(p => p.FormalityScore)
                    .ThenBy(p => p.DiscountPrice ?? p.Price)
                    .FirstOrDefault();

                foreach (var rule in definition.Rules)
                {
                    var candidate = Pick(rule, 0) ?? (rule.IsOptional ? null : Pick(rule, 1));

                    if (candidate != null)
                    {
                        resolvedProducts.Add(candidate);
                        usedIds.Add(candidate.Id);
                    }
                    else if (!rule.IsOptional)
                    {
                        missingCategories.Add(rule.Category.ToLower());
                    }
                }

                // Under three pieces is a pairing, not an outfit - the ranked
                // individual pieces carry the answer instead.
                if (resolvedProducts.Count < 3)
                    continue;

                var totalPrice = resolvedProducts.Sum(p => (decimal)(p.DiscountPrice ?? p.Price));

                // For exact occasion matches, allow up to 3x budget so formal multi-piece
                // outfits (which naturally cost more) still surface with an honest risk flag
                // rather than being silently hidden. Non-matching occasions keep the 2x cap.
                var isExactMatch = string.Equals(definition.OccasionTag, requestedOccasion, StringComparison.OrdinalIgnoreCase);
                var budgetCap = constraints.MaxBudget > 0
                    ? constraints.MaxBudget * (isExactMatch ? 3.0m : 2.0m)
                    : decimal.MaxValue;

                if (totalPrice > budgetCap)
                    continue;

                var riskFlags = new List<string>();
                var scores = new Dictionary<string, double>();
                double score = 0;

                // An incomplete look is offered honestly and ranked below a complete one.
                if (missingCategories.Count > 0)
                {
                    score -= 10 * missingCategories.Count;
                    riskFlags.Add($"Nothing in the catalogue fits the {string.Join(" or ", missingCategories)} slot for this look yet - style it with your own.");
                }

                // Budget scoring
                if (constraints.MaxBudget > 0)
                {
                    if (totalPrice <= constraints.MaxBudget)
                    {
                        score += 40;
                        riskFlags.Add($"Entire outfit fits within your R{constraints.MaxBudget:N0} budget.");
                    }
                    else if (totalPrice <= constraints.MaxBudget * 1.5m)
                    {
                        score += 20;
                        riskFlags.Add($"This look is R{totalPrice:N0} — slightly over your R{constraints.MaxBudget:N0} budget.");
                    }
                    else if (totalPrice <= constraints.MaxBudget * 2.0m)
                    {
                        score += 10;
                        riskFlags.Add($"At R{totalPrice:N0} this is above your R{constraints.MaxBudget:N0} budget — consider it an investment piece.");
                    }
                    else
                    {
                        score += 5;
                        riskFlags.Add($"This complete look is R{totalPrice:N0} — significantly over your R{constraints.MaxBudget:N0} budget, but it's the best match for this occasion.");
                    }
                }

                // Occasion scoring
                if (!string.IsNullOrEmpty(requestedOccasion))
                {
                    if (isExactMatch)
                    {
                        score += 50;
                        riskFlags.Add($"Curated specifically for {requestedOccasion}.");
                    }
                    else if (AreOccasionsAdjacent(definition.OccasionTag, requestedOccasion))
                    {
                        score -= 10;
                    }
                    else
                    {
                        score -= 40;
                    }
                }

                // Anchor item bonus
                if (!string.IsNullOrEmpty(constraints.AnchorItem) &&
                    resolvedProducts.Any(p =>
                        p.Name.Contains(constraints.AnchorItem, StringComparison.OrdinalIgnoreCase) ||
                        p.Tags.Any(t => t.Contains(constraints.AnchorItem, StringComparison.OrdinalIgnoreCase))))
                {
                    score += 30;
                    riskFlags.Add($"Features the {constraints.AnchorItem} you were looking for.");
                }

                scores["Total Match Score"] = Math.Max(0, Math.Min(100, score));

                if (scores["Total Match Score"] < 20)
                    continue;

                results.Add(new Bundle
                {
                    Id = definition.Name.ToLower().Replace(" ", "-"),
                    Name = definition.Name,
                    Occasion = definition.OccasionTag,
                    ItemIds = resolvedProducts.Select(p => p.Id).ToList(),
                    ResolvedProducts = resolvedProducts,
                    TotalPrice = totalPrice,
                    ExplainableScores = scores,
                    RiskFlags = riskFlags
                });
            }

            results.Sort((a, b) =>
                b.ExplainableScores.GetValueOrDefault("Total Match Score", 0)
                 .CompareTo(a.ExplainableScores.GetValueOrDefault("Total Match Score", 0)));

            return results.Take(3).ToList();
        }

        private static bool AreOccasionsAdjacent(string definitionTag, string requestedTag)
        {
            var adjacencyGroups = new List<HashSet<string>>
            {
                new(StringComparer.OrdinalIgnoreCase) { "Interview", "Tech Interview", "Smart Casual", "Graduation" },
                new(StringComparer.OrdinalIgnoreCase) { "Date Night", "Smart Casual", "Casual" },
                new(StringComparer.OrdinalIgnoreCase) { "Casual", "Weekend", "Summer" }
            };

            return adjacencyGroups.Any(g => g.Contains(definitionTag) && g.Contains(requestedTag));
        }
    }
}