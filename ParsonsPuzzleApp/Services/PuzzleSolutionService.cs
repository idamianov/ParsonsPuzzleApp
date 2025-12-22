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

            var map = BuildLetterMaps(puzzle);

            var blocks = model.Arrangement.Where(b => b.Id != 0).ToList();

            var correctEncoded = EncodeCorrectSolution(puzzle, map);

            var studentEncoded = EncodeStudentSolution(blocks, map, puzzle);

            var distance = LevenshteinDistance(correctEncoded, studentEncoded);

            if(distance == 0)
            {
                return true;
            }

            return false;
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

            var map = BuildLetterMaps(puzzle);

            var blocks = model.Arrangement.Where(b => b.Id != 0).ToList();

            var correctEncoded = EncodeCorrectSolution(puzzle, map);

            var studentEncoded = EncodeStudentSolution(blocks, map, puzzle);

            var distance = LevenshteinDistance(correctEncoded, studentEncoded);

            var isCorrect = false;

            if (distance == 0)
            {
                isCorrect = true;
            }

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

        //TODO: remove this code 
        //private bool IsSolutionCorrect(string arrangement, Puzzle puzzle)
        //{
        //    // DEBUGGING: Log the comparison
        //    Debug.WriteLine($"=== DEBUGGING PUZZLE {puzzle.Id} (Language: {puzzle.Language.DisplayName}) ===");
        //    Debug.WriteLine($"Student arrangement:\n{arrangement}");
        //    Debug.WriteLine("--- END Student ---");

        //    // Generate expected solution
        //    string expectedSolution;
        //    var isBracketLanguage = puzzle.Language.IsBracketBased;

        //    // For bracket-based languages AND Python, use SourceCode to preserve structure
        //    // For Python, we need the original indentation
        //    // For SQL languages, we can use PuzzleBlocks
        //    if (isBracketLanguage || puzzle.Language.IsIndentationSensitive ||
        //        puzzle.PuzzleBlocks == null || !puzzle.PuzzleBlocks.Any())
        //    {
        //        expectedSolution = puzzle.SourceCode;
        //        Debug.WriteLine($"Expected from SourceCode (bracket language, Python, or no blocks):\n{expectedSolution}");
        //    }
        //    else
        //    {
        //        // For SQL languages with PuzzleBlocks, generate from blocks
        //        expectedSolution = GenerateExpectedSolutionFromBlocks(puzzle.PuzzleBlocks.ToList(), puzzle.Language);
        //        Debug.WriteLine($"Expected from PuzzleBlocks:\n{expectedSolution}");
        //    }
        //    Debug.WriteLine("--- END Expected ---");

        //    // Replace slots with correct mini-blocks in both arrangement and expected solution
        //    string processedArrangement = arrangement;
        //    string processedExpectedSolution = expectedSolution;

        //    var miniBlocks = puzzle.PuzzleBlocks.SelectMany(pb => pb.Lines).SelectMany(l => l.MiniBlocks).ToList();

        //    foreach (var miniBlock in miniBlocks.Where(mb => mb.IsCorrect))
        //    {
        //        string slotPattern = $"§{miniBlock.SlotName}§";
        //        processedArrangement = processedArrangement.Replace(slotPattern, miniBlock.Content, StringComparison.OrdinalIgnoreCase);
        //        processedExpectedSolution = processedExpectedSolution.Replace(slotPattern, miniBlock.Content, StringComparison.OrdinalIgnoreCase);
        //    }

        //    Debug.WriteLine($"After slot replacement:");
        //    Debug.WriteLine($"Student processed:\n{processedArrangement}");
        //    Debug.WriteLine($"Expected processed:\n{processedExpectedSolution}");
        //    Debug.WriteLine("--- END Processed ---");

        //    // Check if any slots remain unprocessed in student's arrangement
        //    if (Regex.IsMatch(processedArrangement, @"§\w+§"))
        //    {
        //        Debug.WriteLine("❌ FAILED: Unreplaced slots found in student arrangement");
        //        return false; // Unreplaced slots indicate incorrect solution
        //    }

        //    // Validate using the indentation service
        //    bool result = _indentationService.ValidateIndentation(
        //        processedArrangement,
        //        processedExpectedSolution,
        //        puzzle.Language
        //    );

        //    Debug.WriteLine($"Final result: {result}");
        //    Debug.WriteLine("=== END DEBUGGING ===");

        //    return result;
        //}

        //private string GenerateExpectedSolutionFromBlocks(List<PuzzleBlock> puzzleBlocks, Language language)
        //{
        //    Debug.WriteLine($"🔧 GenerateExpectedSolutionFromBlocks called for language: {language}");

        //    var validBlocks = puzzleBlocks
        //        .Where(pb => pb.Lines.All(l => !l.IsDistractor))
        //        .OrderBy(pb => pb.OrderIndex)
        //        .ToList();

        //    Debug.WriteLine($"🔧 Valid blocks count: {validBlocks.Count}");

        //    var lines = new List<string>();

        //    foreach (var block in validBlocks)
        //    {
        //        Debug.WriteLine($"🔧 Processing block: IsMultiline={block.IsMultiline}, Content='{block.Content?.Take(50)}...'");

        //        if (block.IsMultiline)
        //        {
        //            // For multiline blocks, use the content directly (it already contains line breaks)
        //            if (!string.IsNullOrWhiteSpace(block.Content))
        //            {
        //                // Split the content by newlines and add each line
        //                var blockLines = block.Content.Split('\n')
        //                    .Select(l => l.TrimEnd('\r'))
        //                    .ToList();

        //                Debug.WriteLine($"🔧 Adding {blockLines.Count} lines from multiline block content");
        //                lines.AddRange(blockLines);
        //            }
        //        }
        //        else
        //        {
        //            // Handle single-line blocks
        //            if (!string.IsNullOrWhiteSpace(block.Content))
        //            {
        //                Debug.WriteLine($"🔧 Adding single line: '{block.Content}'");
        //                lines.Add(block.Content);
        //            }
        //        }
        //    }

        //    var result = string.Join("\n", lines);
        //    Debug.WriteLine($"🔧 Generated solution:\n{result}");

        //    return result;
        //}

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

        private (Dictionary<int, char>  lineMap, Dictionary<string, char> slotMap) BuildLetterMaps(Puzzle puzzle)
        {
            var lineMap = new Dictionary<int, char>();
            var slotMap = new Dictionary<string, char>();

            char current = 'a';

            foreach (var block in puzzle.PuzzleBlocks.OrderBy(b => b.OrderIndex))
            {
                foreach (var line in block.Lines)
                {
                    lineMap[line.Id] = current++;

                    foreach (var slot in line.MiniBlocks.Where(m => m.IsCorrect))
                    {
                        var key = slot.SlotName + "|" + slot.Content;

                        slotMap[key] = current++;
                    }
                }
            }

            return new (lineMap, slotMap);
        }

        private string EncodeCorrectSolution(Puzzle puzzle, (Dictionary<int, char> lineMap, Dictionary<string, char> slotMap) maps)
        {
            var sb = new StringBuilder();

            foreach (var block in puzzle.PuzzleBlocks.OrderBy(b => b.OrderIndex))
            {
                foreach (var line in block.Lines.Where(l => !l.IsDistractor))
                {
                    char lineLetter = maps.lineMap[line.Id];

                    var slots = line.MiniBlocks.Where(m => m.IsCorrect).ToList();

                    if (slots.Any())
                    {
                        sb.Append("(");
                        sb.Append(lineLetter);

                        foreach (var s in slots)
                        {
                            var key = s.SlotName + "|" + s.Content;
                            sb.Append(maps.slotMap[key]);
                        }

                        sb.Append(")");
                    }
                    else
                    {
                        sb.Append(lineLetter);
                    }

                    sb.Append(block.Indent);
                }
            }

            return sb.ToString();
        }

        private string EncodeStudentSolution(
        List<ArrangementModel> student,
        (Dictionary<int, char> lineMap, Dictionary<string, char> slotMap) maps, Puzzle puzzle)
        {
            var puzzleBlocksById = puzzle.PuzzleBlocks
                .ToDictionary(b => b.Id, b => b);

            var sb = new StringBuilder();

            foreach (var block in student)
            {
                if (!puzzleBlocksById.TryGetValue(block.Id, out var puzzleBlock))
                {
                    continue;
                }

                var puzzleLinesOrdered = puzzleBlock.Lines
                    .OrderBy(l => l.LineOrder)
                    .ToList();

                foreach (var line in block.Lines.OrderBy(l => l.LineIndex))
                {
                    PuzzleBlockLine? puzzleLine = null;

                    puzzleLine = puzzleLinesOrdered[line.LineIndex];

                    char lineLetter = maps.lineMap[puzzleLine.Id];

                    var hasSlots = line.Slots.Any();

                    if (hasSlots)
                    {
                        sb.Append("(");
                        sb.Append(lineLetter);

                        foreach (var slot in line.Slots)
                        {
                            var key = slot.SlotName + "|" + slot.Value;
                            if (!maps.slotMap.ContainsKey(key))
                            {
                                sb.Append('?');
                            }
                            else
                            {
                                sb.Append(maps.slotMap[key]);
                            }
                        }

                        sb.Append(")");
                    }
                    else
                    {
                        sb.Append(lineLetter);
                    }

                    sb.Append(block.Indent);
                }
            }

            return sb.ToString();
        }

        private static uint MIN3(uint a, uint b, uint c)
        {
            return ((a) < (b) ? ((a) < (c) ? (a) : (c)) : ((b) < (c) ? (b) : (c)));
        }

        public static int LevenshteinDistance(string s1, string s2)
        {
            uint s1len, s2len, x, y, lastdiag, olddiag;
            s1len = (uint)s1.Length;
            s2len = (uint)s2.Length;
            uint[] column = new uint[s1len + 1];

            for (y = 1; y <= s1len; ++y)
                column[y] = y;

            for (x = 1; x <= s2len; ++x)
            {
                column[0] = x;

                for (y = 1, lastdiag = x - 1; y <= s1len; ++y)
                {
                    olddiag = column[y];
                    column[y] = MIN3(column[y] + 1, column[y - 1] + 1, (uint)(lastdiag + (s1[(int)(y - 1)] == s2[(int)(x - 1)] ? 0 : 1)));
                    lastdiag = olddiag;
                }
            }

            return (int)(column[s1len]);
        }
    }
}
