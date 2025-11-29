using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ParsonsPuzzleApp.Services
{
    public class PuzzleSolutionService : IPuzzleSolutionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILanguageIndentationService _indentationService;

        public PuzzleSolutionService(ApplicationDbContext context, ILanguageIndentationService indentationService)
        {
            _context = context;
            _indentationService = indentationService;
        }

        public async Task<bool> CheckSolutionAsync(CheckRequestModel model)
        {
            var puzzle = await _context.Puzzles
            .Include(p => p.Language)
            .Include(p => p.PuzzleBlocks)
                .ThenInclude(pb => pb.Lines)
                    .ThenInclude(l => l.MiniBlocks)
            .FirstOrDefaultAsync(p => p.Id == model.PuzzleId);

            if (puzzle == null)
            {
                throw new KeyNotFoundException("Пъзелът не е намерен.");
            }

            if (puzzle.Language == null)
            {
                throw new InvalidOperationException($"Пъзелът {model.PuzzleId} има невалидна или липсваща езикова конфигурация.");
            }

            var blocks = model.Arrangement.Where(b => b.Id != 0).ToList();

            var studentSolution = BuildStudentSolutionString(blocks);

            return IsSolutionCorrect(studentSolution, puzzle);
        }

        public async Task<SubmitSolutionResponse> SubmitSolutionAsync(SubmitSolutionModel model)
        {
            var result = new SubmitSolutionResponse();

            var puzzle = await _context.Puzzles
                .Include(p => p.Language)
                .Include(p => p.PuzzleBlocks)
                    .ThenInclude(pb => pb.Lines)
                        .ThenInclude(l => l.MiniBlocks)
                .FirstOrDefaultAsync(p => p.Id == model.PuzzleId);

            if (puzzle == null)
            {
                throw new KeyNotFoundException("Пъзелът не е намерен.");
            }

            var bundle = await _context.Bundles
                .Include(b => b.BundlePuzzles)
                .FirstOrDefaultAsync(b => b.Id == model.BundleId);

            if (bundle == null)
            {
                throw new KeyNotFoundException("Колекцията не е намерена.");
            }

            bool isCorrect = true;// IsSolutionCorrect(model.Arrangement, puzzle);

            var attempt = new StudentAttempt
            {
                BundleId = model.BundleId,
                PuzzleId = model.PuzzleId,
                StudentIdentifier = model.StudentIdentifier,
                IsCorrect = isCorrect,
                StudentArrangement = string.Empty,
                TimeTakenSeconds = model.TimeTaken,
                AttemptDate = DateTime.UtcNow,
                BundleAttemptId = model.BundleAttemptId
            };

            await _context.StudentAttempts.AddAsync(attempt);
            await _context.SaveChangesAsync();

            await SaveStructuredSolutionAsync(attempt, puzzle, model.Arrangement);

            bool isLast = model.PuzzleIndex >= bundle.BundlePuzzles.Count;

            if (isLast)
            {
                var stats = _context.StudentAttempts
                    .Where(a => a.BundleAttemptId == model.BundleAttemptId)
                    .GroupBy(a => 1)
                    .Select(g => new SolutionStatisticsModel
                    {
                        TotalAttempts = g.Count(),
                        CorrectAttempts = g.Count(a => a.IsCorrect),
                        IncorrectAttempts = g.Count(a => !a.IsCorrect)
                    })
                    .FirstOrDefault();

                var nextUrl = $"/BundleComplete/{model.BundleAttemptId}";

                result = new SubmitSolutionResponse
                {
                    IsLastPuzzle = isLast,
                    IsCorrect = isCorrect,
                    Statistics = stats,
                    NextUrl = nextUrl

                };

                return result;
            }
            else
            {
                var nextUrl = $"/SolvePuzzle?bundleId={model.BundleId}&studentId={model.StudentIdentifier}&puzzleIndex={model.PuzzleIndex + 1}&bundleAttemptId={model.BundleAttemptId}";

                result = new SubmitSolutionResponse
                {
                    IsLastPuzzle = isLast,
                    IsCorrect = isCorrect,
                    Statistics = null,
                    NextUrl = nextUrl
                };

                return result;
            }
        }
        private bool IsSolutionCorrect(string arrangement, Puzzle puzzle)
        {
            // DEBUGGING: Log the comparison
            Debug.WriteLine($"=== DEBUGGING PUZZLE {puzzle.Id} (Language: {puzzle.Language.DisplayName}) ===");
            Debug.WriteLine($"Student arrangement:\n{arrangement}");
            Debug.WriteLine("--- END Student ---");

            // Generate expected solution
            string expectedSolution;
            var isBracketLanguage = puzzle.Language.IsBracketBased;

            // For bracket-based languages AND Python, use SourceCode to preserve structure
            // For Python, we need the original indentation
            // For SQL languages, we can use PuzzleBlocks
            if (isBracketLanguage || puzzle.Language.IsIndentationSensitive ||
                puzzle.PuzzleBlocks == null || !puzzle.PuzzleBlocks.Any())
            {
                expectedSolution = puzzle.SourceCode;
                Debug.WriteLine($"Expected from SourceCode (bracket language, Python, or no blocks):\n{expectedSolution}");
            }
            else
            {
                // For SQL languages with PuzzleBlocks, generate from blocks
                expectedSolution = GenerateExpectedSolutionFromBlocks(puzzle.PuzzleBlocks.ToList(), puzzle.Language);
                Debug.WriteLine($"Expected from PuzzleBlocks:\n{expectedSolution}");
            }
            Debug.WriteLine("--- END Expected ---");

            // Replace slots with correct mini-blocks in both arrangement and expected solution
            string processedArrangement = arrangement;
            string processedExpectedSolution = expectedSolution;

            var miniBlocks = puzzle.PuzzleBlocks.SelectMany(pb => pb.Lines).SelectMany(l => l.MiniBlocks).ToList();

            foreach (var miniBlock in miniBlocks.Where(mb => mb.IsCorrect))
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

        private string GenerateExpectedSolutionFromBlocks(List<PuzzleBlock> puzzleBlocks, Language language)
        {
            Debug.WriteLine($"🔧 GenerateExpectedSolutionFromBlocks called for language: {language}");

            var validBlocks = puzzleBlocks
                .Where(pb => pb.Lines.All(l => !l.IsDistractor))
                .OrderBy(pb => pb.OrderIndex)
                .ToList();

            Debug.WriteLine($"🔧 Valid blocks count: {validBlocks.Count}");

            var lines = new List<string>();

            foreach (var block in validBlocks)
            {
                Debug.WriteLine($"🔧 Processing block: IsMultiline={block.IsMultiline}, Content='{block.Content?.Take(50)}...'");

                if (block.IsMultiline)
                {
                    // For multiline blocks, use the content directly (it already contains line breaks)
                    if (!string.IsNullOrWhiteSpace(block.Content))
                    {
                        // Split the content by newlines and add each line
                        var blockLines = block.Content.Split('\n')
                            .Select(l => l.TrimEnd('\r'))
                            .ToList();

                        Debug.WriteLine($"🔧 Adding {blockLines.Count} lines from multiline block content");
                        lines.AddRange(blockLines);
                    }
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
            Debug.WriteLine($"🔧 Generated solution:\n{result}");

            return result;
        }

        public async Task SaveStructuredSolutionAsync(
        StudentAttempt attempt,
        Puzzle puzzle,
        List<ArrangementModel> arrangement)
        {
            var puzzleBlocksById = puzzle.PuzzleBlocks
                .ToDictionary(b => b.Id, b => b);

            int blockPosition = 0;

            var studentBlocks = arrangement.Where(b => b.Id != 0).ToList();

            foreach (var studentBlock in studentBlocks)
            {
                blockPosition++;

                if (!puzzleBlocksById.TryGetValue(studentBlock.Id, out var puzzleBlock))
                {
                    continue;
                }

                var blockAttempt = new StudentAttemptBlock
                {
                    StudentAttemptId = attempt.Id,
                    PuzzleBlockId = puzzleBlock.Id,
                    Position = blockPosition,
                    Indent = studentBlock.Indent
                };

                _context.StudentAttemptBlocks.Add(blockAttempt);
                await _context.SaveChangesAsync();

                var puzzleLinesOrdered = puzzleBlock.Lines
                    .OrderBy(l => l.LineOrder)
                    .ToList();

                int linePosition = 0;

                foreach (var studentLine in studentBlock.Lines)
                {
                    linePosition++;

                    PuzzleBlockLine? puzzleLine = null;

                    puzzleLine = puzzleLinesOrdered[studentLine.LineIndex];

                    if (puzzleLine == null)
                    {
                        continue;
                    }

                    var lineAttempt = new StudentAttemptBlockLine
                    {
                        StudentAttemptBlockId = blockAttempt.Id,
                        PuzzleBlockLineId = puzzleLine.Id,
                        Position = linePosition,
                        Content = studentLine.Text
                    };

                    _context.StudentAttemptBlockLines.Add(lineAttempt);
                    await _context.SaveChangesAsync();

                    var puzzleMiniBlocks = puzzleLine.MiniBlocks.ToList();

                    int miniPosition = 0;

                    foreach (var studentSlot in studentLine.Slots)
                    {
                        miniPosition++;

                        var puzzleMiniBlock = puzzleMiniBlocks
                            .FirstOrDefault(s => s.SlotName == studentSlot.SlotName && s.Content == studentSlot.Value);

                        var miniAttempt = new StudentAttemptMiniBlock
                        {
                            StudentAttemptBlockLineId = lineAttempt.Id,
                            MiniBlockId = puzzleMiniBlock.Id,
                            Position = miniPosition,
                        };

                        _context.StudentAttemptMiniBlocks.Add(miniAttempt);
                    }

                    await _context.SaveChangesAsync();
                }
            }
        }

        public static string BuildStudentSolutionString(List<ArrangementModel> arrangement)
        {
            var sb = new StringBuilder();

            foreach (var block in arrangement)
            {
                int indentSpaces = block.Indent * 2;

                foreach (var line in block.Lines.OrderBy(l => l.LineIndex))
                {
                    string processedText = line.Text;

                    sb.AppendLine(new string(' ', indentSpaces) + processedText);
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}
