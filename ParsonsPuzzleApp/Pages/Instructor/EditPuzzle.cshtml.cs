namespace ParsonsPuzzleApp.Pages.Instructor
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;
    using ParsonsPuzzleApp.Data;
    using ParsonsPuzzleApp.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    [Authorize]
    public class EditPuzzleModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditPuzzleModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Puzzle Puzzle { get; set; }

        [BindProperty]
        public Dictionary<string, List<MiniBlockInput>> MiniBlocks { get; set; } = new Dictionary<string, List<MiniBlockInput>>();

        public List<SelectListItem> LanguageOptions { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Puzzle = await _context.Puzzles
                .Include(p => p.MiniBlocks)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Puzzle == null)
            {
                return NotFound();
            }

            LanguageOptions = Enum.GetValues(typeof(Languages))
                .Cast<Languages>()
                .Select(l => new SelectListItem
                {
                    Value = ((int)l).ToString(),
                    Text = l.ToString()
                })
                .ToList();

            MiniBlocks = Puzzle.MiniBlocks
                .GroupBy(mb => mb.SlotName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(mb => new MiniBlockInput { Content = mb.Content }).ToList());

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LanguageOptions = Enum.GetValues(typeof(Languages))
                    .Cast<Languages>()
                    .Select(l => new SelectListItem
                    {
                        Value = ((int)l).ToString(),
                        Text = l.ToString()
                    })
                    .ToList();
                return Page();
            }

            var slotRegex = new Regex(@"§([^§]+)§");
            var slots = slotRegex.Matches(Puzzle.SourceCode)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            foreach (var slot in slots)
            {
                if (!MiniBlocks.ContainsKey(slot) || MiniBlocks[slot].Count == 0)
                {
                    ModelState.AddModelError("", $"Слотът '{slot}' няма дефинирани мини-блокове.");
                    LanguageOptions = Enum.GetValues(typeof(Languages))
                        .Cast<Languages>()
                        .Select(l => new SelectListItem
                        {
                            Value = ((int)l).ToString(),
                            Text = l.ToString()
                        })
                        .ToList();
                    return Page();
                }
            }

            _context.Attach(Puzzle).State = EntityState.Modified;

            var existingMiniBlocks = _context.MiniBlocks.Where(mb => mb.PuzzleId == Puzzle.Id);
            _context.MiniBlocks.RemoveRange(existingMiniBlocks);

            foreach (var slot in MiniBlocks)
            {
                for (int i = 0; i < slot.Value.Count; i++)
                {
                    _context.MiniBlocks.Add(new MiniBlock
                    {
                        PuzzleId = Puzzle.Id,
                        SlotName = slot.Key,
                        Content = slot.Value[i].Content,
                        IsCorrect = i == 0
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("/Instructor/Puzzles");
        }

        public class MiniBlockInput
        {
            public string Content { get; set; }
        }
    }
}