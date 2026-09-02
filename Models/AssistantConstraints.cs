namespace INF4027W_BPTTIN002_MiniPrj_2026.Models
{
    /// <summary>
    /// Structured constraints extracted from the user's natural language prompt
    /// or uploaded clothing image by the AI service.
    /// Drives the deterministic ranking engine in the Assistant page model.
    /// </summary>
    public class AssistantConstraints
    {       
        /// <summary>
        /// True when ConfidenceLevel is 1 or 2 — the assistant must ask
        /// a clarifying question before generating results.
        /// False ONLY when ConfidenceLevel is 3.
        /// </summary>
        public bool NeedsMoreInfo { get; set; } = false;

        /// <summary>
        /// A single, warm clarifying question targeting the most critical
        /// missing constraint. Empty when NeedsMoreInfo is false.
        /// Priority order: Gender → Occasion → Budget.
        /// </summary>
        public string ClarifyingQuestion { get; set; } = string.Empty;

        /// <summary>
        /// Confidence that enough information has been gathered to recommend.
        /// 1 = gender unknown OR both occasion and budget unknown.
        /// 2 = gender known BUT occasion OR budget still missing.
        /// 3 = gender + occasion + budget ALL explicitly stated.
        /// </summary>
        public int ConfidenceLevel { get; set; } = 1;

        /// <summary>
        /// Target gender for product filtering: "Men" | "Women" | "" (unknown).
        /// Empty string triggers a clarifying question.
        /// </summary>
        public string Gender { get; set; } = string.Empty;

  

        /// <summary>
        /// False only when the prompt has nothing to do with the store — triggers the
        /// decline path. Store questions (sizing, delivery, returns) are related.
        /// </summary>
        public bool IsFashionRelated { get; set; } = true;

        /// <summary>
        /// True when the customer asked a question to be answered rather than a brief
        /// to shop — sizing, fabric care, store policy, "why this one?". Skips the
        /// ranking engine and replies conversationally.
        /// </summary>
        public bool AnswerDirectly { get; set; } = false;

        /// <summary>
        /// The occasion context extracted from the prompt e.g. "Interview", "Date Night".
        /// Normalised to canonical values for bundle matching.
        /// </summary>
        public string OccasionContext { get; set; } = string.Empty;

        /// <summary>
        /// Target formality level on a 1–5 scale inferred from the occasion.
        /// 1 = Very Casual, 5 = Very Formal. 0 = not specified.
        /// </summary>
        public int TargetFormality { get; set; } = 0;

        /// <summary>
        /// Maximum budget in Rands. 0 means not stated — sentinel value.
        /// There is NO default budget. Zero triggers a clarifying question.
        /// </summary>
        public decimal MaxBudget { get; set; } = 0m;

        /// <summary>
        /// A specific item the user mentioned wanting to include e.g. "white shirt".
        /// For image uploads this is set to the identified item name.
        /// Empty if no anchor item was mentioned.
        /// </summary>
        public string AnchorItem { get; set; } = string.Empty;

        /// <summary>
        /// A short, human-readable summary of what was understood from the prompt.
        /// Used internally — never rendered directly to the customer (XSS guard).
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// The primary clothing category the user is searching for.
        /// Must be one of: Tops | Bottoms | Dresses | Skirts | Jackets | Shoes | Outerwear | Accessories
        /// Empty when the user is describing an occasion rather than a specific item.
        /// </summary>
        public string SearchCategory { get; set; } = string.Empty;

        /// <summary>
        /// True when the user's query is focused on a specific product type
        /// rather than a complete outfit.
        /// </summary>
        public bool IsProductSearch { get; set; } = false;

        /// <summary>
        /// True when the customer uploaded a clothing image.
        /// </summary>
        public bool HasImageInput { get; set; } = false;

        /// <summary>
        /// Gemini Vision's description of the uploaded item.
        /// </summary>
        public string UploadedItemDescription { get; set; } = string.Empty;

        /// <summary>
        /// Clothing category inferred from the image.
        /// </summary>
        public string UploadedItemCategory { get; set; } = string.Empty;

        /// <summary>
        /// Dominant colour family of the uploaded item in lowercase.
        /// </summary>
        public string UploadedItemColour { get; set; } = string.Empty;

        /// <summary>
        /// Formality score of the uploaded item on a 1–5 scale.
        /// </summary>
        public int UploadedItemFormality { get; set; } = 3;

        /// <summary>
        /// Set to true by the ranking engine when a catalogue match is found.
        /// Always returned as false by the AI - the engine resolves this.
        /// </summary>
        public bool UploadedItemFoundInCatalogue { get; set; } = false;
    }
}