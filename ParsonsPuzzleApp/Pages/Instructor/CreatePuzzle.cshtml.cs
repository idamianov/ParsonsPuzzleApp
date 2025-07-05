namespace ParsonsPuzzleApp.Pages.Instructor
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using ParsonsPuzzleApp.Data;
    using ParsonsPuzzleApp.Models;
    using ParsonsPuzzleApp.Services;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    [Authorize]
    public class CreatePuzzleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IMultilineBlockParser _blockParser;

        public CreatePuzzleModel(ApplicationDbContext context, IMultilineBlockParser blockParser)
        {
            _context = context;
            _blockParser = blockParser;
        }

        [BindProperty]
        public Puzzle Puzzle { get; set; }

        [BindProperty]
        public Dictionary<string, List<MiniBlockInput>> MiniBlocks { get; set; } = new Dictionary<string, List<MiniBlockInput>>();

        public List<SelectListItem> LanguageOptions { get; set; }

        public void OnGet()
        {
            LanguageOptions = Enum.GetValues(typeof(Languages))
                .Cast<Languages>()
                .Select(l => new SelectListItem
                {
                    Value = ((int)l).ToString(),
                    Text = l.ToString()
                })
                .ToList();
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

            // Валидация на блокове в кода
            if (!MultilineBlockValidator.ValidateBlockSyntax(Puzzle.SourceCode, Puzzle.Language))
            {
                ModelState.AddModelError("Puzzle.SourceCode", "Има незатворени многоредови блокове в кода!");
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

            // Извличане на слотове от SourceCode
            var slotRegex = new Regex(@"§([^§]+)§");
            var slots = slotRegex.Matches(Puzzle.SourceCode)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            // Валидация на MiniBlocks
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

            // Запазване на пъзела
            _context.Puzzles.Add(Puzzle);
            await _context.SaveChangesAsync();

            // Парсване и създаване на PuzzleBlocks от SourceCode
            var puzzleBlocks = _blockParser.ParseSourceCode(
                Puzzle.SourceCode,
                Puzzle.Id,
                Puzzle.Language
            );

            foreach (var block in puzzleBlocks)
            {
                _context.PuzzleBlocks.Add(block);
                await _context.SaveChangesAsync();

                // Ако е многоредов блок, създаваме линиите
                if (block.IsMultiline && !string.IsNullOrWhiteSpace(block.Content))
                {
                    var lines = block.Content.Split('\n')
                        .Select(l => l.TrimEnd('\r'))
                        .ToList();

                    for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                    {
                        var line = lines[lineIndex];
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            _context.PuzzleBlockLines.Add(new PuzzleBlockLine
                            {
                                PuzzleBlockId = block.Id,
                                Content = line,
                                LineOrder = lineIndex,
                                IsOptional = false
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }

            // Обработка на дистрактори
            if (!string.IsNullOrEmpty(Puzzle.Distractors))
            {
                // Валидация на блокове в дистракторите
                if (!MultilineBlockValidator.ValidateBlockSyntax(Puzzle.Distractors, Puzzle.Language))
                {
                    // Ако има грешки в дистракторите, третираме ги като обикновени редове
                    var distractorLines = Puzzle.Distractors.Split('\n')
                        .Where(l => !string.IsNullOrWhiteSpace(l.Trim()));

                    int orderIndex = puzzleBlocks.Count;
                    foreach (var line in distractorLines)
                    {
                        _context.PuzzleBlocks.Add(new PuzzleBlock
                        {
                            PuzzleId = Puzzle.Id,
                            Content = line.Trim(),
                            BlockType = "single",
                            IsMultiline = false,
                            IsOrderIndependent = false,
                            OrderIndex = orderIndex++,
                            IsDistractor = true
                        });
                    }
                }
                else
                {
                    // Парсваме дистракторите като блокове
                    var distractorBlocks = _blockParser.ParseSourceCode(
                        Puzzle.Distractors,
                        Puzzle.Id,
                        Puzzle.Language
                    );

                    foreach (var distractor in distractorBlocks)
                    {
                        distractor.IsDistractor = true;
                        distractor.OrderIndex += puzzleBlocks.Count;
                        _context.PuzzleBlocks.Add(distractor);
                    }
                }
                await _context.SaveChangesAsync();
            }

            // Добавяне на MiniBlocks
            foreach (var slot in MiniBlocks)
            {
                for (int i = 0; i < slot.Value.Count; i++)
                {
                    _context.MiniBlocks.Add(new MiniBlock
                    {
                        PuzzleId = Puzzle.Id,
                        SlotName = slot.Key,
                        Content = slot.Value[i].Content,
                        IsCorrect = i == 0 // Първият е верен
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