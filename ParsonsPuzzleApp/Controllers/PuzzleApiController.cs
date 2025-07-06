using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Models;
using ParsonsPuzzleApp.Services;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace ParsonsPuzzleApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PuzzleApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILanguageIndentationService _indentationService;

        public PuzzleApiController(ApplicationDbContext context, ILanguageIndentationService indentationService)
        {
            _context = context;
            _indentationService = indentationService;
        }

        [HttpPost("check")]
        public IActionResult CheckSolution([FromBody] CheckRequestModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Arrangement) || model.PuzzleId <= 0)
            {
                return BadRequest("Invalid request data.");
            }

            var puzzle = _context.Puzzles
                .Include(p => p.MiniBlocks)
                .Include(p => p.PuzzleBlocks)
                .ThenInclude(pb => pb.Lines)
                .FirstOrDefault(p => p.Id == model.PuzzleId);

            if (puzzle == null)
            {
                return NotFound("Пъзелът не е намерен.");
            }

            bool isCorrect = IsSolutionCorrect(model.Arrangement, puzzle);
            return Ok(new { isCorrect });
        }

        [HttpPost("submit")]
        public IActionResult SubmitSolution([FromBody] SubmitSolutionModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Arrangement) || model.PuzzleId <= 0 ||
                model.BundleId <= 0 || string.IsNullOrEmpty(model.StudentIdentifier) || model.BundleAttemptId == Guid.Empty)
            {
                return BadRequest("Invalid request data.");
            }

            var puzzle = _context.Puzzles
                .Include(p => p.MiniBlocks)
                .Include(p => p.PuzzleBlocks)
                .ThenInclude(pb => pb.Lines)
                .FirstOrDefault(p => p.Id == model.PuzzleId);

            if (puzzle == null)
            {
                return NotFound("Пъзелът не е намерен.");
            }

            var bundle = _context.Bundles
                .Include(b => b.BundlePuzzles)
                .FirstOrDefault(b => b.Id == model.BundleId);

            if (bundle == null)
            {
                return NotFound("Колекцията не е намерена.");
            }

            bool isCorrect = IsSolutionCorrect(model.Arrangement, puzzle);

            var attempt = new StudentAttempt
            {
                BundleId = model.BundleId,
                PuzzleId = model.PuzzleId,
                StudentIdentifier = model.StudentIdentifier,
                IsCorrect = isCorrect,
                StudentArrangement = model.Arrangement,
                TimeTakenSeconds = model.TimeTaken,
                AttemptDate = DateTime.UtcNow,
                BundleAttemptId = model.BundleAttemptId
            };

            _context.StudentAttempts.Add(attempt);
            _context.SaveChanges();

            var totalPuzzles = bundle.BundlePuzzles.Count;
            bool isLast = model.PuzzleIndex >= totalPuzzles;

            if (isLast)
            {
                var stats = _context.StudentAttempts
                    .Where(a => a.BundleAttemptId == model.BundleAttemptId)
                    .GroupBy(a => 1)
                    .Select(g => new
                    {
                        TotalAttempts = g.Count(),
                        CorrectAttempts = g.Count(a => a.IsCorrect),
                        IncorrectAttempts = g.Count(a => !a.IsCorrect)
                    })
                    .FirstOrDefault() ?? new { TotalAttempts = 0, CorrectAttempts = 0, IncorrectAttempts = 0 };

                var nextUrl = "/SelectBundle";

                return Ok(new
                {
                    isLast = true,
                    isCorrect,
                    statistics = new
                    {
                        stats.TotalAttempts,
                        stats.CorrectAttempts,
                        stats.IncorrectAttempts
                    },
                    nextUrl
                });
            }
            else
            {
                var nextUrl = $"/SolvePuzzle?bundleId={model.BundleId}&studentId={model.StudentIdentifier}&puzzleIndex={model.PuzzleIndex + 1}&bundleAttemptId={model.BundleAttemptId}";
                return Ok(new { isLast = false, isCorrect, nextUrl });
            }
        }

        private bool IsSolutionCorrect(string arrangement, Puzzle puzzle)
        {
            // DEBUGGING: Log the comparison
            Debug.WriteLine($"=== DEBUGGING PUZZLE {puzzle.Id} (Language: {puzzle.Language}) ===");
            Debug.WriteLine($"Student arrangement:\n{arrangement}");
            Debug.WriteLine("--- END Student ---");

            // CRITICAL FIX: For bracket-based languages, always use SourceCode to preserve structure
            string expectedSolution;
            var isBracketLanguage = IsBracketBasedLanguage(puzzle.Language);

            if (isBracketLanguage)
            {
                // For bracket languages, use original SourceCode to preserve indentation structure
                expectedSolution = puzzle.SourceCode;
                Debug.WriteLine($"Expected from SourceCode (bracket language):\n{expectedSolution}");
            }
            else if (puzzle.PuzzleBlocks != null && puzzle.PuzzleBlocks.Any())
            {
                // For non-bracket languages, use PuzzleBlocks system
                expectedSolution = GenerateExpectedSolutionFromBlocks(puzzle.PuzzleBlocks, puzzle.Language);
                Debug.WriteLine($"Expected from PuzzleBlocks:\n{expectedSolution}");
            }
            else
            {
                // Fallback to SourceCode
                expectedSolution = puzzle.SourceCode;
                Debug.WriteLine($"Expected from SourceCode (fallback):\n{expectedSolution}");
            }
            Debug.WriteLine("--- END Expected ---");

            // Replace slots with correct mini-blocks in both arrangement and expected solution
            string processedArrangement = arrangement;
            string processedExpectedSolution = expectedSolution;

            foreach (var miniBlock in puzzle.MiniBlocks.Where(mb => mb.IsCorrect))
            {
                string slotPattern = $"§{miniBlock.SlotName}§";
                processedArrangement = processedArrangement.Replace(slotPattern, miniBlock.Content, StringComparison.OrdinalIgnoreCase);
                processedExpectedSolution = processedExpectedSolution.Replace(slotPattern, miniBlock.Content, StringComparison.OrdinalIgnoreCase);
            }

            Debug.WriteLine($"After slot replacement:");
            Debug.WriteLine($"Student processed:\n{processedArrangement}");
            Debug.WriteLine($"Expected processed:\n{processedExpectedSolution}");
            Debug.WriteLine("--- END Processed ---");

            // Check if any slots remain unprocessed in student's arrangement
            if (Regex.IsMatch(processedArrangement, @"§\w+§"))
            {
                Debug.WriteLine("❌ FAILED: Unreplaced slots found in student arrangement");
                return false; // Unreplaced slots indicate incorrect solution
            }

            // Validate using the indentation service
            bool result = _indentationService.ValidateIndentation(
                processedArrangement,
                processedExpectedSolution,
                puzzle.Language
            );

            Debug.WriteLine($"Final result: {result}");
            Debug.WriteLine("=== END DEBUGGING ===");

            return result;
        }

        private string GenerateExpectedSolutionFromBlocks(List<PuzzleBlock> puzzleBlocks, Languages language)
        {
            Debug.WriteLine($"🔧 GenerateExpectedSolutionFromBlocks called for language: {language}");

            // Filter out bracket blocks for C-family languages
            var isBracketLanguage = IsBracketBasedLanguage(language);
            Debug.WriteLine($"🔧 Is bracket language: {isBracketLanguage}");

            var validBlocks = puzzleBlocks
                .Where(pb => !pb.IsDistractor) // Skip distractors
                .Where(pb => !(isBracketLanguage && (pb.Content?.Trim() == "{" || pb.Content?.Trim() == "}"))) // Skip bracket blocks for C-family
                .OrderBy(pb => pb.OrderIndex)
                .ToList();

            Debug.WriteLine($"🔧 Valid blocks count: {validBlocks.Count}");

            var lines = new List<string>();

            foreach (var block in validBlocks)
            {
                Debug.WriteLine($"🔧 Processing block: IsMultiline={block.IsMultiline}, Content='{block.Content?.Take(50)}...'");

                if (block.IsMultiline && block.Lines != null && block.Lines.Any())
                {
                    // Handle multiline blocks - lines are always in order regardless of IsOrderIndependent
                    var blockLines = block.Lines
                        .OrderBy(l => l.LineOrder)
                        .Select(l => l.Content)
                        .ToList();

                    Debug.WriteLine($"🔧 Adding {blockLines.Count} lines from multiline block");
                    lines.AddRange(blockLines);
                }
                else
                {
                    // Handle single-line blocks
                    if (!string.IsNullOrWhiteSpace(block.Content))
                    {
                        Debug.WriteLine($"🔧 Adding single line: '{block.Content}'");
                        lines.Add(block.Content);
                    }
                }
            }

            var result = string.Join("\n", lines);
            Debug.WriteLine($"🔧 Before adding braces:\n{result}");

            // CRITICAL FIX: For bracket-based languages, add braces based on indentation structure
            if (isBracketLanguage)
            {
                Debug.WriteLine("🔧 Adding braces for bracket-based language...");
                result = AddBracesBasedOnIndentation(result);
                Debug.WriteLine($"🔧 After adding braces:\n{result}");
            }

            return result;
        }

        private string AddBracesBasedOnIndentation(string code)
        {
            Debug.WriteLine($"🔧 AddBracesBasedOnIndentation input:\n{code}");

            var lines = code.Split('\n').ToList();
            var result = new List<string>();
            var braceStack = new Stack<int>();

            Debug.WriteLine($"🔧 Processing {lines.Count} lines...");

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var currentIndent = GetIndentationLevel(line);

                Debug.WriteLine($"🔧 Line {i}: '{line}' -> indent level: {currentIndent}");

                if (i > 0)
                {
                    var prevIndent = GetIndentationLevel(lines[i - 1]);
                    Debug.WriteLine($"🔧 Previous indent: {prevIndent}, Current indent: {currentIndent}");

                    // Close braces if indentation decreased
                    while (braceStack.Count > 0 && braceStack.Peek() >= currentIndent)
                    {
                        var braceIndent = braceStack.Pop();
                        var closeBrace = new string(' ', braceIndent * 2) + "}";
                        Debug.WriteLine($"🔧 Adding closing brace: '{closeBrace}'");
                        result.Add(closeBrace);
                    }

                    // Open brace if indentation increased
                    if (currentIndent > prevIndent)
                    {
                        var openBrace = new string(' ', prevIndent * 2) + "{";
                        Debug.WriteLine($"🔧 Adding opening brace: '{openBrace}'");
                        result.Add(openBrace);
                        braceStack.Push(currentIndent);
                    }
                }
                else if (currentIndent > 0)
                {
                    // First line with indentation
                    Debug.WriteLine($"🔧 First line with indentation, adding opening brace");
                    result.Add("{");
                    braceStack.Push(currentIndent);
                }

                result.Add(line);
            }

            // Close any remaining open braces
            while (braceStack.Count > 0)
            {
                var braceIndent = braceStack.Pop();
                var closeBrace = new string(' ', braceIndent * 2) + "}";
                Debug.WriteLine($"🔧 Adding final closing brace: '{closeBrace}'");
                result.Add(closeBrace);
            }

            var finalResult = string.Join("\n", result);
            Debug.WriteLine($"🔧 AddBracesBasedOnIndentation output:\n{finalResult}");
            return finalResult;
        }

        private int GetIndentationLevel(string line)
        {
            int spaces = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ' ')
                    spaces++;
                else if (line[i] == '\t')
                    spaces += 4; // Assume 4 spaces per tab
                else
                    break;
            }
            return spaces / 2; // Assuming 2 spaces per indentation level
        }

        private bool IsBracketBasedLanguage(Languages language)
        {
            return language == Languages.C ||
                   language == Languages.Cpp ||
                   language == Languages.CSharp ||
                   language == Languages.Java ||
                   language == Languages.JavaScript;
        }

        public class CheckRequestModel
        {
            public int PuzzleId { get; set; }
            public string Arrangement { get; set; }
        }

        public class SubmitSolutionModel
        {
            public int BundleId { get; set; }
            public int PuzzleId { get; set; }
            public int PuzzleIndex { get; set; }
            public string StudentIdentifier { get; set; }
            public string Arrangement { get; set; }
            public int TimeTaken { get; set; }
            public Guid BundleAttemptId { get; set; }
        }
    }
}