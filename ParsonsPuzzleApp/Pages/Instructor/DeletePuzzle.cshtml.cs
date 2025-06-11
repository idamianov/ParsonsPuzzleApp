namespace ParsonsPuzzleApp.Pages.Instructor
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using ParsonsPuzzleApp.Data;
    using ParsonsPuzzleApp.Models;
    using System.Threading.Tasks;

    [Authorize]
    public class DeletePuzzleModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeletePuzzleModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Puzzle Puzzle { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Puzzle = await _context.Puzzles
                .Include(p => p.BundlePuzzles)
                .ThenInclude(bp => bp.Bundle)
                .Include(p => p.MiniBlocks)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Puzzle == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Puzzle = await _context.Puzzles
                .Include(p => p.BundlePuzzles)
                .Include(p => p.MiniBlocks)
                .FirstOrDefaultAsync(p => p.Id == Puzzle.Id);

            if (Puzzle == null)
            {
                return NotFound();
            }

            // Премахване на свързани записи
            _context.BundlePuzzles.RemoveRange(Puzzle.BundlePuzzles);
            _context.MiniBlocks.RemoveRange(Puzzle.MiniBlocks);
            _context.Puzzles.Remove(Puzzle);

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Пъзелът беше успешно изтрит!";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Грешка при изтриване на пъзела. Моля, опитайте отново.";
                return Page();
            }

            return RedirectToPage("./Puzzles");
        }
    }
}