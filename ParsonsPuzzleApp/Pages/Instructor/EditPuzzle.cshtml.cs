using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Services;
using System.Text.RegularExpressions;

namespace ParsonsPuzzleApp.Pages.Instructor
{
    [Authorize]
    public class EditPuzzleModel : PageModel
    {
        private static readonly Regex SlotRegex = new Regex(@"§([^§]+)§", RegexOptions.Compiled);
        private readonly ApplicationDbContext _context;
        private readonly IMultilineBlockParser _blockParser;
        private readonly UserManager<IdentityUser> _userManager;

        public EditPuzzleModel(ApplicationDbContext context, IMultilineBlockParser blockParser, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _blockParser = blockParser;
            _userManager = userManager;
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

            var userId = _userManager.GetUserId(User);
            Puzzle = await _context.Puzzles
                .Include(p => p.Language)
                .Include(p => p.PuzzleBlocks)
                    .ThenInclude(pb => pb.Lines)
                        .ThenInclude(l => l.MiniBlocks)
                .FirstOrDefaultAsync(m => m.Id == id && m.InstructorId == userId);

            if (Puzzle == null)
            {
                return NotFound();
            }

            LanguageOptions = await GetLanguageOptionsAsync();

            var miniBlocks = Puzzle.PuzzleBlocks
                .SelectMany(pb => pb.Lines)
                .SelectMany(l => l.MiniBlocks)
                .ToList();

            // Load existing MiniBlocks
            MiniBlocks = miniBlocks
                .GroupBy(mb => mb.SlotName)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(mb => mb.IsCorrect ? 0 : 1) // Correct ones first
                        .Select(mb => new MiniBlockInput { Content = mb.Content })
                        .ToList());

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remove InstructorId from ModelState to prevent validation errors
            ModelState.Remove("Puzzle.InstructorId");
            ModelState.Remove("Puzzle.Language");

            if (!ModelState.IsValid)
            {
                LanguageOptions = await GetLanguageOptionsAsync();
                return Page();
            }

            // Load language entity for validation
            var language = await _context.Languages.FindAsync(Puzzle.LanguageId);
            if (language == null)
            {
                ModelState.AddModelError("Puzzle.LanguageId", "Невалиден език.");
                LanguageOptions = await GetLanguageOptionsAsync();
                return Page();
            }

            // Validate multiline block syntax
            if (!MultilineBlockValidator.ValidateBlockSyntax(Puzzle.SourceCode, language))
            {
                ModelState.AddModelError("Puzzle.SourceCode", "Има незатворени многоредови блокове в кода!");
                LanguageOptions = await GetLanguageOptionsAsync();
                return Page();
            }

            // Extract and validate slots
            var slots = ExtractSlotsFromCode(Puzzle.SourceCode);
            foreach (var slot in slots)
            {
                if (!MiniBlocks.ContainsKey(slot) || MiniBlocks[slot].Count == 0)
                {
                    ModelState.AddModelError("", $"Слотът '{slot}' няма дефинирани мини-блокове.");
                    LanguageOptions = await GetLanguageOptionsAsync();
                    return Page();
                }
            }

            // Verify ownership
            var userId = _userManager.GetUserId(User);
            var existingPuzzle = await _context.Puzzles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == Puzzle.Id && p.InstructorId == userId);

            if (existingPuzzle == null)
            {
                return NotFound();
            }

            // Update puzzle basic info
            Puzzle.InstructorId = userId;
            Puzzle.LastModifiedAt = DateTime.UtcNow;
            _context.Attach(Puzzle).State = EntityState.Modified;

            var existingPuzzleBlocks = _context.PuzzleBlocks.Where(pb => pb.PuzzleId == Puzzle.Id);
            _context.PuzzleBlocks.RemoveRange(existingPuzzleBlocks);

            await _context.SaveChangesAsync();

            // Recreate PuzzleBlocks from updated SourceCode
            var puzzleBlocks = _blockParser.ParseSourceCode(
                Puzzle.SourceCode,
                Puzzle.Id,
                language
            );

            // Filter out bracket blocks for C-family languages
            var isBracketLanguage = language.IsBracketBased;
            var validBlocks = puzzleBlocks.Where(pb =>
                !(isBracketLanguage && (pb.Content?.Trim() == "{" || pb.Content?.Trim() == "}"))
            ).ToList();

            var createdLines = new List<(int LineId, string Content)>();

            foreach (var block in validBlocks)
            {
                _context.PuzzleBlocks.Add(block);
                await _context.SaveChangesAsync();

                await CreateLinesForBlockAsync(block, createdLines);
            }

            // Process distractors
            await ProcessDistractors(validBlocks.Count, language);

            var slotIndex = IndexSlotsByLine(createdLines);

            // 5) Create MiniBlocks under PuzzleBlockLineId
            await AddMiniBlocks(slotIndex);

            await _context.SaveChangesAsync();
            return RedirectToPage("/Instructor/Puzzles");
        }

        private async Task CreateLinesForBlockAsync(PuzzleBlock block, List<(int LineId, string Content)> createdLines)
        {
            // Multiline: split into multiple lines
            if (block.IsMultiline && !string.IsNullOrWhiteSpace(block.Content))
            {
                var lines = block.Content.Split('\n')
                    .Select(l => l.TrimEnd('\r'))
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();

                for (int i = 0; i < lines.Count; i++)
                {
                    var lineEntity = new PuzzleBlockLine
                    {
                        PuzzleBlockId = block.Id,
                        Content = lines[i],
                        LineOrder = i,
                        IsOptional = false
                    };
                    _context.PuzzleBlockLines.Add(lineEntity);
                    await _context.SaveChangesAsync(); // need Line Id now
                    createdLines.Add((lineEntity.Id, lineEntity.Content));
                }
            }
            else
            {
                // Single-line: create one line for the block content
                var lineEntity = new PuzzleBlockLine
                {
                    PuzzleBlockId = block.Id,
                    Content = block.Content,
                    LineOrder = 0,
                    IsOptional = false
                };
                _context.PuzzleBlockLines.Add(lineEntity);
                await _context.SaveChangesAsync();
                createdLines.Add((lineEntity.Id, lineEntity.Content ?? string.Empty));
            }
        }

        private Dictionary<string, List<int>> IndexSlotsByLine(IEnumerable<(int LineId, string Content)> lines)
        {
            var map = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            foreach (var (lineId, content) in lines)
            {
                if (string.IsNullOrEmpty(content)) continue;

                foreach (Match m in SlotRegex.Matches(content))
                {
                    var slot = m.Groups[1].Value;
                    if (!map.TryGetValue(slot, out var list))
                    {
                        list = new List<int>();
                        map[slot] = list;
                    }
                    if (!list.Contains(lineId))
                        list.Add(lineId);
                }
            }
            return map;
        }

        private async Task AddMiniBlocks(Dictionary<string, List<int>> slotIndex)
        {
            foreach (var kvp in MiniBlocks)
            {
                var slotName = kvp.Key;
                if (!slotIndex.TryGetValue(slotName, out var lineIds) || lineIds.Count == 0)
                {
                    continue;
                }

                foreach (var lineId in lineIds)
                {
                    for (int i = 0; i < kvp.Value.Count; i++)
                    {
                        _context.MiniBlocks.Add(new MiniBlock
                        {
                            PuzzleBlockLineId = lineId,
                            SlotName = slotName,
                            Content = kvp.Value[i].Content,
                            IsCorrect = i == 0 // first is correct
                        });
                    }
                }
            }
        }

        private List<string> ExtractSlotsFromCode(string sourceCode)
        {
            var slotRegex = new Regex(@"§([^§]+)§");
            return slotRegex.Matches(sourceCode)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();
        }

        private async Task ProcessDistractors(int baseOrderIndex, Language language)
        {
            if (string.IsNullOrWhiteSpace(Puzzle.Distractors))
                return;

            var isBracketLanguage = language.IsBracketBased;

            // Treat distractors as simple single-line blocks
            var distractorLines = Puzzle.Distractors.Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l) &&
                            !(isBracketLanguage && (l == "{" || l == "}")))
                .ToList();

            int orderIndex = baseOrderIndex;
            foreach (var line in distractorLines)
            {
                var block = new PuzzleBlock
                {
                    PuzzleId = Puzzle.Id,
                    Content = line,
                    BlockType = "single",
                    IsMultiline = false,
                    IsOrderIndependent = false,
                    OrderIndex = orderIndex++,
                };

                _context.PuzzleBlocks.Add(block);
                await _context.SaveChangesAsync(); // need Block.Id

                _context.PuzzleBlockLines.Add(new PuzzleBlockLine
                {
                    PuzzleBlockId = block.Id,
                    Content = block.Content,
                    IsDistractor = true,
                    LineOrder = 0,
                    IsOptional = false
                });

                await _context.SaveChangesAsync();
            }
        }

        private async Task<List<SelectListItem>> GetLanguageOptionsAsync()
        {
            var languages = await _context.Languages
                .Where(l => l.IsActive)
                .OrderBy(l => l.SortOrder)
                .ToListAsync();

            return languages.Select(l => new SelectListItem
            {
                Value = l.Id.ToString(),
                Text = l.DisplayName
            }).ToList();
        }

        public class MiniBlockInput
        {
            public string Content { get; set; }
        }
    }
}