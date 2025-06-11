using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParsonsPuzzleApp.Pages.Instructor
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using ParsonsPuzzleApp.Data;
    using ParsonsPuzzleApp.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    [Authorize]
    public class PuzzlesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PuzzlesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Puzzle> Puzzles { get; set; }

        public async Task OnGetAsync()
        {
            Puzzles = await _context.Puzzles
                .Include(p => p.BundlePuzzles)
                .ThenInclude(bp => bp.Bundle)
                .ToListAsync();
        }
    }
}
