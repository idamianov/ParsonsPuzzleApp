using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Models;
using System;
using System.Linq;

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

            var puzzle = _context.Puzzles.Find(model.PuzzleId);
            if (puzzle == null)
            {
                return NotFound("Пъзелът не е намерен.");
            }

            bool isCorrect = string.Equals(model.Arrangement.Trim(), puzzle.SourceCode.Trim(), StringComparison.OrdinalIgnoreCase);
            return Ok(new { isCorrect });
        }

        [HttpPost("submit")]
        public IActionResult SubmitSolution([FromBody] SubmitSolutionModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Arrangement) || model.PuzzleId <= 0 || model.BundleId <= 0 || string.IsNullOrEmpty(model.StudentIdentifier) || model.BundleAttemptId == Guid.Empty)
            {
                return BadRequest("Invalid request data.");
            }

            var puzzle = _context.Puzzles.Find(model.PuzzleId);
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

            bool isCorrect = string.Equals(model.Arrangement.Trim(), puzzle.SourceCode.Trim(), StringComparison.OrdinalIgnoreCase);

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