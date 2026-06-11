using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Helpers;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Services;
using System.Text.RegularExpressions;

namespace ParsonsPuzzleApp.Pages.Instructor
{
    [Authorize]
    public class CreatePuzzleModel : PageModel
    {
        private static readonly Regex SlotRegex = new Regex(@"§([^§]+)§", RegexOptions.Compiled);
        private readonly ApplicationDbContext _context;
        private readonly IMultilineBlockParser _blockParser;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHtmlSanitizerService _htmlSanitizer;

        public CreatePuzzleModel(ApplicationDbContext context, IMultilineBlockParser blockParser,
            UserManager<IdentityUser> userManager, IHtmlSanitizerService htmlSanitizer)
        {
            _context = context;
            _blockParser = blockParser;
            _userManager = userManager;
            _htmlSanitizer = htmlSanitizer;
        }

        [BindProperty]
        public Puzzle Puzzle { get; set; } = new Puzzle();

        [BindProperty]
        public Dictionary<string, List<MiniBlockInput>> MiniBlocks { get; set; } = new Dictionary<string, List<MiniBlockInput>>();

        public List<SelectListItem> LanguageOptions { get; set; }

        public async Task OnGetAsync()
        {
            LanguageOptions = await GetLanguageOptionsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Set the InstructorId before validation
            Puzzle.InstructorId = _userManager.GetUserId(User);
            Puzzle.EncodedSolution = string.Empty;

            // Sanitize HTML in Task field
            if (!string.IsNullOrWhiteSpace(Puzzle.Task))
            {
                if (_htmlSanitizer.ContainsDangerousContent(Puzzle.Task))
                {
                    // Log the attempt for security monitoring
                    var userId = _userManager.GetUserId(User);
                    Console.WriteLine($"WARNING: User {userId} attempted to submit dangerous HTML content");
                }

                Puzzle.Task = _htmlSanitizer.SanitizeHtml(Puzzle.Task);
            }

            // Remove InstructorId from ModelState to prevent validation errors
            ModelState.Remove("Puzzle.InstructorId");
            ModelState.Remove("Puzzle.Language");
            ModelState.Remove("Puzzle.EncodedSolution");

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

            // Validate multiline block syntax in source code
            if (!MultilineBlockValidator.ValidateBlockSyntax(Puzzle.SourceCode, language))
            {
                ModelState.AddModelError("Puzzle.SourceCode", "Има незатворени многоредови блокове в кода!");
                LanguageOptions = await GetLanguageOptionsAsync();
                return Page();
            }

            // Extract slots from SourceCode
            var slots = ExtractSlotsFromCode(Puzzle.SourceCode);

            // Validate MiniBlocks for each slot
            foreach (var slot in slots)
            {
                if (!MiniBlocks.ContainsKey(slot) || MiniBlocks[slot].Count == 0)
                {
                    ModelState.AddModelError("", $"Слотът '{slot}' няма дефинирани мини-блокове.");
                    LanguageOptions = await GetLanguageOptionsAsync();
                    return Page();
                }
            }

            // Save puzzle
            _context.Puzzles.Add(Puzzle);
            await _context.SaveChangesAsync();

            // Parse and create PuzzleBlocks from SourceCode
            var puzzleBlocks = _blockParser.ParseSourceCode(
                Puzzle.SourceCode,
                Puzzle.Id,
                language
            );

            // Filter out bracket blocks for C-family languages during creation
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

            await AddMiniBlocks(slotIndex);

            await _context.SaveChangesAsync();

            var map = PuzzleEncoderHelper.BuildLetterMaps(Puzzle);

            var correctEncoded = PuzzleEncoderHelper.EncodeCorrectSolution(Puzzle, map);

            Puzzle.EncodedSolution = correctEncoded;

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


        private List<string> ExtractSlotsFromCode(string sourceCode)
        {
            return SlotRegex.Matches(sourceCode)
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