using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Interfaces;

namespace ParsonsPuzzleApp.Pages
{
    public class BundleAccessModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IBundleAccessService _bundleAccessService;

        public BundleAccessModel(ApplicationDbContext context, IBundleAccessService bundleAccessService)
        {
            _context = context;
            _bundleAccessService = bundleAccessService;
        }

        public Bundle Bundle { get; set; }
        public List<Language> Languages { get; set; } = new List<Language>();
        public int PuzzleCount { get; set; }
        public List<string> PuzzleTitles { get; set; } = new List<string>();

        [BindProperty]
        public string StudentIdentifier { get; set; }

        [BindProperty]
        public string BundleCode { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid shareableLink)
        {
            Bundle = await _context.Bundles
                .FirstOrDefaultAsync(b => b.ShareableLink == shareableLink && b.IsPublished);

            if (Bundle == null)
            {
                // Redirect to a better error page instead of just returning NotFound
                return RedirectToPage("/BundleNotFound", new { shareableLink });
            }

            Languages = await _context.BundlePuzzles
                .Where(bp => bp.BundleId == Bundle.Id)
                .Select(bp => bp.Puzzle.Language)
                .Distinct()
                .OrderBy(l => l.DisplayName)
                .ToListAsync();

            var puzzles = await _context.BundlePuzzles
                .Where(bp => bp.BundleId == Bundle.Id)
                .Select(bp => bp.Puzzle.Title)
                .ToListAsync();
            
            PuzzleCount = puzzles.Count;
            PuzzleTitles = puzzles;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid shareableLink)
        {
            Bundle = await _context.Bundles
                .FirstOrDefaultAsync(b => b.ShareableLink == shareableLink && b.IsPublished);

            if (Bundle == null)
            {
                return RedirectToPage("/BundleNotFound", new { shareableLink });
            }

            if (!ModelState.IsValid)
            {
                Languages = await _context.BundlePuzzles
                    .Where(bp => bp.BundleId == Bundle.Id)
                    .Select(bp => bp.Puzzle.Language)
                    .Distinct()
                    .OrderBy(l => l.DisplayName)
                    .ToListAsync();

                var puzzles = await _context.BundlePuzzles
                    .Where(bp => bp.BundleId == Bundle.Id)
                    .Select(bp => bp.Puzzle.Title)
                    .ToListAsync();
                
                PuzzleCount = puzzles.Count;
                PuzzleTitles = puzzles;

                return Page();
            }

            // Verify the unlock code
            if (Bundle.Key != BundleCode)
            {
                ModelState.AddModelError("BundleCode", "Невалиден код за отключване.");
                // Add a more visible error message
                TempData["ErrorMessage"] = "Въведеният код за отключване е грешен. Моля, опитайте отново.";
                
                Languages = await _context.BundlePuzzles
                    .Where(bp => bp.BundleId == Bundle.Id)
                    .Select(bp => bp.Puzzle.Language)
                    .Distinct()
                    .OrderBy(l => l.DisplayName)
                    .ToListAsync();

                var puzzles = await _context.BundlePuzzles
                    .Where(bp => bp.BundleId == Bundle.Id)
                    .Select(bp => bp.Puzzle.Title)
                    .ToListAsync();
                
                PuzzleCount = puzzles.Count;
                PuzzleTitles = puzzles;

                return Page();
            }

            // Grant access to the bundle
            _bundleAccessService.GrantAccess(Bundle.Id, StudentIdentifier);

            // Generate new attempt ID and redirect to puzzle solving
            var bundleAttemptId = Guid.NewGuid();
            return RedirectToPage("/SolvePuzzle", new
            {
                bundleId = Bundle.Id,
                studentId = StudentIdentifier,
                puzzleIndex = 1,
                bundleAttemptId
            });
        }
    }
}