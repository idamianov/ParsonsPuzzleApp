using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ParsonsPuzzleApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PuzzleApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PuzzleApiController(ApplicationDbContext context)
        {
            _context = context;
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
            if (model == null || string.IsNullOrEmpty(model.Arrangement) || model.PuzzleId <= 0 || model.BundleId <= 0 || string.IsNullOrEmpty(model.StudentIdentifier) || model.BundleAttemptId == Guid.Empty)
            {
                return BadRequest("Invalid request data.");
            }

            var puzzle = _context.Puzzles
                .Include(p => p.MiniBlocks)
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
                return NotFound("Бънделът не е намерен.");
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

                return Ok(new
                {
                    isLast = true,
                    isCorrect,
                    statistics = new
                    {
                        stats.TotalAttempts,
                        stats.CorrectAttempts,
                        stats.IncorrectAttempts
                    }
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
            // Step 1: Replace slots with correct mini-blocks
            string processedArrangement = arrangement;
            string processedSourceCode = puzzle.SourceCode;

            foreach (var miniBlock in puzzle.MiniBlocks.Where(mb => mb.IsCorrect))
            {
                string slotPattern = $"§{miniBlock.SlotName}§";
                processedArrangement = processedArrangement.Replace(slotPattern, miniBlock.Content, StringComparison.OrdinalIgnoreCase);
                processedSourceCode = processedSourceCode.Replace(slotPattern, miniBlock.Content, StringComparison.OrdinalIgnoreCase);
            }

            // Check if any slots remain unprocessed
            if (Regex.IsMatch(processedArrangement, @"§\w+§"))
            {
                return false; // Unreplaced slots indicate incorrect solution
            }

            // Step 2: Normalize based on language
            if (puzzle.Language.ToString().ToLower() == "python")
            {
                // Preserve indentation, trim leading/trailing empty lines
                processedArrangement = string.Join("\n", processedArrangement.Split('\n')
                    .Select(l => l.TrimEnd())
                    .Where(l => !string.IsNullOrWhiteSpace(l)));
                processedSourceCode = string.Join("\n", processedSourceCode.Split('\n')
                    .Select(l => l.TrimEnd())
                    .Where(l => !string.IsNullOrWhiteSpace(l)));
            }
            else
            {
                // Remove leading whitespace/tabs, keep braces, remove empty lines
                processedArrangement = string.Join("\n", processedArrangement.Split('\n')
                    .Select(l => l.TrimStart())
                    .Where(l => !string.IsNullOrWhiteSpace(l)));
                processedSourceCode = string.Join("\n", processedSourceCode.Split('\n')
                    .Select(l => l.TrimStart())
                    .Where(l => !string.IsNullOrWhiteSpace(l)));
            }

            // Normalize new lines
            processedArrangement = processedArrangement.Replace("\r\n", "\n").Trim();
            processedSourceCode = processedSourceCode.Replace("\r\n", "\n").Trim();

            // Step 3: Compare normalized codes
            return string.Equals(processedArrangement, processedSourceCode, StringComparison.OrdinalIgnoreCase);
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