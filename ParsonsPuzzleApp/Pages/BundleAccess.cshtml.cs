using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Models;
using ParsonsPuzzleApp.Services;
using System;
using System.Threading.Tasks;

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
                return NotFound("Колекцията не е намерена или не е публикувана.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid shareableLink)
        {
            Bundle = await _context.Bundles
                .FirstOrDefaultAsync(b => b.ShareableLink == shareableLink && b.IsPublished);

            if (Bundle == null)
            {
                return NotFound("Колекцията не е намерена или не е публикувана.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Verify the unlock code
            if (Bundle.Key != BundleCode)
            {
                ModelState.AddModelError("BundleCode", "Невалиден код за отключване.");
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