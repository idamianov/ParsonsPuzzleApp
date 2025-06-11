namespace ParsonsPuzzleApp.Pages.Instructor
{
    using global::ParsonsPuzzleApp.Data;
    using global::ParsonsPuzzleApp.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;
    using ParsonsPuzzleApp.Data;
    using ParsonsPuzzleApp.Models;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [Authorize]
    public class EditBundleModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditBundleModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Bundle Bundle { get; set; }
        public List<SelectListItem> PuzzleOptions { get; set; }
        [BindProperty]
        public List<int> SelectedPuzzleIds { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Bundle = await _context.Bundles
                .Include(b => b.BundlePuzzles)
                .ThenInclude(bp => bp.Puzzle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Bundle == null)
            {
                return NotFound();
            }

            PuzzleOptions = await _context.Puzzles
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Title
                })
                .ToListAsync();

            SelectedPuzzleIds = Bundle.BundlePuzzles.Select(bp => bp.PuzzleId).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                PuzzleOptions = await _context.Puzzles
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.Title
                    })
                    .ToListAsync();
                return Page();
            }

            // Валидация за поне един избран пъзел
            if (SelectedPuzzleIds == null || !SelectedPuzzleIds.Any())
            {
                ModelState.AddModelError("", "Моля, изберете поне един пъзел за пакета.");
                PuzzleOptions = await _context.Puzzles
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.Title
                    })
                    .ToListAsync();
                return Page();
            }

            var bundleToUpdate = await _context.Bundles
                .Include(b => b.BundlePuzzles)
                .FirstOrDefaultAsync(b => b.Id == Bundle.Id);

            if (bundleToUpdate == null)
            {
                return NotFound();
            }

            // Актуализиране на основните свойства
            bundleToUpdate.Identifier = Bundle.Identifier;
            bundleToUpdate.Key = Bundle.Key;
            bundleToUpdate.Description = Bundle.Description;

            // Премахване на старите връзки с пъзели
            var existingPuzzles = _context.BundlePuzzles.Where(bp => bp.BundleId == Bundle.Id);
            _context.BundlePuzzles.RemoveRange(existingPuzzles);

            // Добавяне на новите връзки с пъзели
            foreach (var puzzleId in SelectedPuzzleIds)
            {
                _context.BundlePuzzles.Add(new BundlePuzzle
                {
                    BundleId = Bundle.Id,
                    PuzzleId = puzzleId
                });
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Пакетът беше успешно актуализиран!";
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Грешка при запазване на пакета. Моля, опитайте отново.");
                PuzzleOptions = await _context.Puzzles
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.Title
                    })
                    .ToListAsync();
                return Page();
            }

            return RedirectToPage("./Bundles");
        }
    }
}