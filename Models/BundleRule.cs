namespace INF4027W_BPTTIN002_MiniPrj_2026.Models
{
    /// <summary>
    /// A single slot rule within a bundle definition.
    /// Specifies the required category and acceptable formality range.
    /// </summary>
    public class BundleRule
    {
        public string Category { get; set; } = string.Empty;
        public int MinFormality { get; set; }
        public int MaxFormality { get; set; }
        public bool IsOptional { get; set; } = false;
    }

    /// <summary>
    /// A static blueprint for generating outfit bundles.
    /// Each definition targets a specific gender + occasion combination
    /// and contains ordered rules that are filled from the product catalogue.
    /// </summary>
    public class BundleDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string OccasionTag { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<BundleRule> Rules { get; set; } = new();
    }
}