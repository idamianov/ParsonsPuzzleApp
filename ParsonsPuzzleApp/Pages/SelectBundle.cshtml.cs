using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParsonsPuzzleApp.Pages
{
    public class SelectBundleModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SelectBundleModel(ApplicationDbContext context)
        {
            _context = context;
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
            Bundles = await _context.Bundles.ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(StudentIdentifier))
            {
                ModelState.AddModelError("StudentIdentifier", "Моля, въведете идентификатор.");
                Bundles = await _context.Bundles.ToListAsync();
                return Page();
            }

            var bundle = await _context.Bundles.FirstOrDefaultAsync(b => b.Id == SelectedBundleId);
            if (bundle == null || bundle.Key != BundleCode)
            {
                ModelState.AddModelError("BundleCode", "Невалиден код за бъндела.");
                Bundles = await _context.Bundles.ToListAsync();
                return Page();
            }

            var bundleAttemptId = Guid.NewGuid();
            return RedirectToPage("/SolvePuzzle", new { bundleId = SelectedBundleId, studentId = StudentIdentifier, puzzleIndex = 1, bundleAttemptId });
        }
    }
}