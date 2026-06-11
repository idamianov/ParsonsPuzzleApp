using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Helpers;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;
using System.Text;

namespace ParsonsPuzzleApp.Services
{
    public class PuzzleSolutionService : IPuzzleSolutionService
    {
        private readonly ApplicationDbContext _context;

        public PuzzleSolutionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> CheckSolutionAsync(CheckRequestModel model)
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

            var map = PuzzleEncoderHelper.BuildLetterMaps(puzzle);

            var blocks = model.Arrangement.Where(b => b.Id != 0).ToList();

            var studentEncoded = EncodeStudentSolution(blocks, map, puzzle);

            var distance = LevenshteinDistance(puzzle.EncodedSolution, studentEncoded);

            return distance;
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

            var map = PuzzleEncoderHelper.BuildLetterMaps(puzzle);

            var blocks = model.Arrangement.Where(b => b.Id != 0).ToList();

            var studentEncoded = EncodeStudentSolution(blocks, map, puzzle);

            var distance = LevenshteinDistance(puzzle.EncodedSolution, studentEncoded);

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

        private string EncodeStudentSolution(
        List<ArrangementModel> student,
        (Dictionary<int, char> lineMap, Dictionary<string, char> slotMap) maps, Puzzle puzzle)
        {
            var puzzleBlocksById = puzzle.PuzzleBlocks
                .ToDictionary(b => b.Id, b => b);

            NormalizeIndentation(student);

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

                    // For SQL-based languages indentation doesn't affect correctness at all
                    if (puzzle.Language.IsSqlBased)
                    {
                        block.Indent = puzzleBlock.Indent;
                    }

                    sb.Append(block.Indent);
                }
            }

            return sb.ToString();
        }

        private void NormalizeIndentation(List<ArrangementModel> student)
        {
            if (!student.Any())
                return;

            while (true)
            {
                bool allHaveIndentation = student.All(block => block.Indent > 0);

                if (!allHaveIndentation)
                    break;

                foreach (var block in student)
                {
                    block.Indent--;
                }
            }
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

        private static uint MIN3(uint a, uint b, uint c)
        {
            return ((a) < (b) ? ((a) < (c) ? (a) : (c)) : ((b) < (c) ? (b) : (c)));
        }
    }
}
