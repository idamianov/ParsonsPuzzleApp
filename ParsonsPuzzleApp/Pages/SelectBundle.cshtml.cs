using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Models;
using ParsonsPuzzleApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParsonsPuzzleApp.Pages
{
    public class SelectBundleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IBundleAccessService _bundleAccessService;

        public SelectBundleModel(ApplicationDbContext context, IBundleAccessService bundleAccessService)
        {
            _context = context;
            _bundleAccessService = bundleAccessService;
        }

        public List<Bundle> Bundles { get; set; }

        [BindProperty]
        public int SelectedBundleId { get; set; }

        [BindProperty]
        public string StudentIdentifier { get; set; }

        [BindProperty]
        public string BundleCode { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Only show published bundles
            Bundles = await _context.Bundles
                .Where(b => b.IsPublished)
                .ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(StudentIdentifier))
            {
                ModelState.AddModelError("StudentIdentifier", "Моля, въведете идентификатор.");
                Bundles = await _context.Bundles.Where(b => b.IsPublished).ToListAsync();
                return Page();
            }

            var bundle = await _context.Bundles.FirstOrDefaultAsync(b => b.Id == SelectedBundleId && b.IsPublished);
            if (bundle == null || bundle.Key != BundleCode)
            {
                ModelState.AddModelError("BundleCode", "Невалиден код за колекцията или колекцията не е публикувана.");
                Bundles = await _context.Bundles.Where(b => b.IsPublished).ToListAsync();
                return Page();
            }

            // Grant access to the bundle
            _bundleAccessService.GrantAccess(bundle.Id, StudentIdentifier);

            var bundleAttemptId = Guid.NewGuid();
            return RedirectToPage("/SolvePuzzle", new { bundleId = SelectedBundleId, studentId = StudentIdentifier, puzzleIndex = 1, bundleAttemptId });
        }
    }
}