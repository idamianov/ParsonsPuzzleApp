using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Pages.Instructor
{
    public class CreateBundleModel : PageModel
    {
        [BindProperty]
        public Bundle Bundle { get; set; }

        public List<SelectListItem> PuzzleOptions { get; set; }

        private readonly ApplicationDbContext _context;

        public CreateBundleModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public List<int> SelectedPuzzleIds { get; set; }

        public void OnGet()
        {
            PuzzleOptions = _context.Puzzles
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Title
                })
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Bundles.Add(Bundle);
            await _context.SaveChangesAsync();

            // Добавяне на връзките в BundlePuzzle
            if (SelectedPuzzleIds != null)
            {
                foreach (var puzzleId in SelectedPuzzleIds)
                {
                    _context.BundlePuzzles.Add(new BundlePuzzle
                    {
                        BundleId = Bundle.Id,
                        PuzzleId = puzzleId
                    });
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Bundles");
        }
    }
}
