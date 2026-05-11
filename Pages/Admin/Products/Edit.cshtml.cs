using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using INF4027W_BPTTIN002_MiniPrj_2026.Models;
using INF4027W_BPTTIN002_MiniPrj_2026.Services;

namespace INF4027W_BPTTIN002_MiniPrj_2026.Pages.Admin.Products
{
    public class EditModel : PageModel
    {
        private readonly FirestoreService _firestore;

        [BindProperty] public Product Product { get; set; } = new();
        [BindProperty] public List<string> SelectedSizes { get; set; } = new();
        [BindProperty] public List<string> SelectedOccasionTags { get; set; } = new();
        [BindProperty] public string TagsRaw { get; set; } = string.Empty;

        public EditModel(FirestoreService firestore)
        {
            _firestore = firestore;
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToPage("/Admin/Products/Index");

            var product = await _firestore.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            Product = product;
            TagsRaw = string.Join(",", product.Tags);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            Product.Sizes = SelectedSizes;
            Product.OccasionTags = SelectedOccasionTags;

            // Unchecked checkboxes are not submitted by browsers, so read
            // the raw form values directly to correctly persist boolean toggles.
            Product.IsDraft = Request.Form["Product.IsDraft"].Contains("true");
            Product.IsHiddenFromWeb = Request.Form["Product.IsHiddenFromWeb"].Contains("true");

            Product.Tags = string.IsNullOrWhiteSpace(TagsRaw)
                ? new List<string>()
                : TagsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(t => t.Trim())
                         .Where(t => !string.IsNullOrEmpty(t))
                         .ToList();

            await _firestore.UpdateProductAsync(Product);
            TempData["Success"] = $"'{Product.Name}' was updated successfully.";
            return RedirectToPage("/Admin/Products/Index");
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                await _firestore.DeleteProductAsync(id);
                TempData["Success"] = "Product deleted.";
            }
            return RedirectToPage("/Admin/Products/Index");
        }
    }
}