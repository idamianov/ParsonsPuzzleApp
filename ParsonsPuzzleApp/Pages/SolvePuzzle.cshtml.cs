namespace ParsonsPuzzleApp.Pages
{
    using Microsoft.AspNetCore.Mvc;
        using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using ParsonsPuzzleApp.Data;
    using ParsonsPuzzleApp.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class SolvePuzzleModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SolvePuzzleModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Bundle Bundle { get; set; }
        public Puzzle Puzzle { get; set; }
        public int PuzzleIndex { get; set; }
        public int TotalPuzzles { get; set; }
        public string StudentIdentifier { get; set; }
        public Guid BundleAttemptId { get; set; }
        public List<CodeBlock> ShuffledCodeBlocks { get; set; }
        public List<MiniBlockConfig> MiniBlocks { get; set; }

        public async Task<IActionResult> OnGetAsync(int bundleId, string studentId, int puzzleIndex, Guid? bundleAttemptId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return RedirectToPage("SelectBundle");
            }

            if (!bundleAttemptId.HasValue || bundleAttemptId == Guid.Empty)
            {
                return BadRequest("Липсва валиден BundleAttemptId.");
            }

            StudentIdentifier = studentId;
            BundleAttemptId = bundleAttemptId.Value;

            Bundle = await _context.Bundles
                .Include(b => b.BundlePuzzles)
                .ThenInclude(bp => bp.Puzzle)
                .ThenInclude(p => p.MiniBlocks)
                .FirstOrDefaultAsync(b => b.Id == bundleId);

            if (Bundle == null)
            {
                return NotFound("Колекцията не е намерена.");
            }

            TotalPuzzles = Bundle.BundlePuzzles.Count;
            if (puzzleIndex < 1 || puzzleIndex > TotalPuzzles)
            {
                return NotFound("Невалиден индекс на пъзел.");
            }

            Puzzle = Bundle.BundlePuzzles
                .OrderBy(bp => bp.PuzzleId)
                .Skip(puzzleIndex - 1)
                .FirstOrDefault()?.Puzzle;

            if (Puzzle == null)
            {
                return NotFound("Пъзелът не е намерен.");
            }

            PuzzleIndex = puzzleIndex;

            ShuffledCodeBlocks = ParseSourceCode(Puzzle)
                .OrderBy(x => Random.Shared.Next())
                .ToList();

            MiniBlocks = Puzzle.MiniBlocks.Select(mb => new MiniBlockConfig
            {
                Content = mb.Content,
                SlotName = mb.SlotName,
                IsCorrect = mb.IsCorrect
            }).ToList();

            return Page();
        }

        public class CodeBlock
        {
            public string Content { get; set; }
            public string SlotName { get; set; }
            public bool IsDistractor { get; set; }
        }

        public class MiniBlockConfig
        {
            public string Content { get; set; }
            public string SlotName { get; set; }
            public bool IsCorrect { get; set; }
        }

        private List<CodeBlock> ParseSourceCode(Puzzle puzzle)
        {
            var lines = puzzle.SourceCode.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.TrimEnd('\r'))
                .ToList();
            var blocks = new List<CodeBlock>();

            foreach (var line in lines)
            {
                string content = line.Trim();
                if (string.IsNullOrWhiteSpace(content) || content == "{" || content == "}") continue;

                string slotName = null;
                var match = Regex.Match(content, @"§(\w+)§");
                if (match.Success)
                {
                    slotName = match.Groups[1].Value;
                }

                blocks.Add(new CodeBlock { Content = content, SlotName = slotName, IsDistractor = false });
            }

            if (!string.IsNullOrEmpty(puzzle.Distractors))
            {
                var distractors = puzzle.Distractors.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim())
                    .Where(d => !string.IsNullOrWhiteSpace(d));
                blocks.AddRange(distractors.Select(d => new CodeBlock { Content = d, SlotName = null, IsDistractor = true }));
            }

            return blocks;
        }
    }
}