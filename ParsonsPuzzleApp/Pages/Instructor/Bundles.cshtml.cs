using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Pages.Instructor
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    [Authorize]
    public class BundlesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BundlesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Bundle> Bundles { get; set; }

        public async Task OnGetAsync()
        {
            Bundles = await _context.Bundles
                .Include(b => b.BundlePuzzles)
                .ThenInclude(bp => bp.Puzzle)
                .ToListAsync();
        }
    }
}
