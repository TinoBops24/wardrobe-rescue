using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;
using Google.Cloud.Firestore;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Admin.Products
{
    public class CreateModel : PageModel
    {
        private readonly FirestoreService _firestore;
        private readonly ILogger<CreateModel> _logger;

        [BindProperty] public Product Product { get; set; } = new();
        [BindProperty] public List<string> SelectedSizes { get; set; } = new();
        [BindProperty] public List<string> SelectedOccasionTags { get; set; } = new();
        [BindProperty] public string TagsRaw { get; set; } = string.Empty;

        public CreateModel(FirestoreService firestore, ILogger<CreateModel> logger)
        {
            _firestore = firestore;
            _logger = logger;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    if (error.Value?.Errors.Count > 0)
                        _logger.LogWarning("ModelState error — {Key}: {Error}",
                            error.Key, error.Value.Errors[0].ErrorMessage);
                }
                return Page();
            }

            Product.Sizes = SelectedSizes;
            Product.OccasionTags = SelectedOccasionTags;
            Product.CreatedAt = Timestamp.GetCurrentTimestamp();

            // HTML checkboxes are not submitted when unchecked, so we explicitly
            // read the form values to correctly distinguish checked from unchecked.
            Product.IsDraft = Request.Form["Product.IsDraft"].Contains("true");
            Product.IsHiddenFromWeb = Request.Form["Product.IsHiddenFromWeb"].Contains("true");

            Product.Tags = string.IsNullOrWhiteSpace(TagsRaw)
                ? new List<string>()
                : TagsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(t => t.Trim())
                         .Where(t => !string.IsNullOrEmpty(t))
                         .ToList();

            // Clear any auto-generated ID — FirestoreService generates the document ID
            Product.Id = string.Empty;

            await _firestore.CreateProductAsync(Product);
            TempData["Success"] = $"'{Product.Name}' was added successfully.";
            return RedirectToPage("/Admin/Products/Index");
        }
    }
}