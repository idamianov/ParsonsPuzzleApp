namespace ParsonsPuzzleApp.Pages.Instructor
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using ParsonsPuzzleApp.Data;
    using ParsonsPuzzleApp.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    [Authorize]
    public class CreatePuzzleModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        [BindProperty]
        public List<PuzzleBlockViewModel> PuzzleBlocks { get; set; } = new List<PuzzleBlockViewModel>();

        public CreatePuzzleModel(ApplicationDbContext context)
        {
            _context = context;
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

        public IActionResult OnPost()
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

            _context.Puzzles.Add(Puzzle);
            _context.SaveChanges();

            // Обработка на PuzzleBlocks ако има такива
            if (PuzzleBlocks != null && PuzzleBlocks.Any())
            {
                for (int i = 0; i < PuzzleBlocks.Count; i++)
                {
                    var blockVm = PuzzleBlocks[i];
                    if (blockVm == null) continue;

                    var puzzleBlock = new PuzzleBlock
                    {
                        PuzzleId = Puzzle.Id,
                        Content = blockVm.Content ?? string.Empty,
                        GroupId = blockVm.GroupId,
                        BlockType = blockVm.BlockType,
                        IsMultiline = blockVm.IsMultiline,
                        IsOrderIndependent = blockVm.OrderIndependent,
                        OrderIndex = i,
                        IsDistractor = false
                    };

                    _context.PuzzleBlocks.Add(puzzleBlock);
                    _context.SaveChanges();

                    // Ако е многоредов, добавяме редовете
                    if (blockVm.IsMultiline && blockVm.Lines != null)
                    {
                        for (int j = 0; j < blockVm.Lines.Count; j++)
                        {
                            var line = blockVm.Lines[j];
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                _context.PuzzleBlockLines.Add(new PuzzleBlockLine
                                {
                                    PuzzleBlockId = puzzleBlock.Id,
                                    Content = line,
                                    LineOrder = j,
                                    IsOptional = false
                                });
                            }
                        }
                        _context.SaveChanges();
                    }
                }
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

            _context.SaveChanges();

            return RedirectToPage("/Instructor/Puzzles");
        }

        public class MiniBlockInput
        {
            public string Content { get; set; }
        }

        public class PuzzleBlockViewModel
        {
            public string Content { get; set; }
            public string GroupId { get; set; }
            public string BlockType { get; set; }
            public bool IsMultiline { get; set; }
            public bool OrderIndependent { get; set; }
            public List<string> Lines { get; set; } = new List<string>();
        }
    }
}