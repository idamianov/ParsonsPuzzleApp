using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Constants;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Pages
{
    public class BundleCompleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILtiAgsService _agsService;
        private readonly ILogger<BundleCompleteModel> _logger;

        public BundleCompleteModel(
            ApplicationDbContext context,
            ILtiAgsService agsService,
            ILogger<BundleCompleteModel> logger)
        {
            _context = context;
            _agsService = agsService;
            _logger = logger;
        }

        public Bundle Bundle { get; set; }
        public string StudentIdentifier { get; set; }
        public DateTime CompletedAt { get; set; }
        public BundleStatistics Statistics { get; set; }
        public List<PuzzleResultViewModel> PuzzleResults { get; set; }
        public string? ReturnUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid bundleAttemptId)
        {
            // Get all attempts for this bundle attempt
            var attempts = await _context.StudentAttempts
                .Where(a => a.BundleAttemptId == bundleAttemptId)
                .Include(a => a.Bundle)
                .Include(a => a.Puzzle)
                .OrderBy(a => a.AttemptDate)
                .ToListAsync();

            if (!attempts.Any())
            {
                return NotFound("Резултатите не са намерени.");
            }

            var firstAttempt = attempts.First();
            Bundle = firstAttempt.Bundle;
            StudentIdentifier = firstAttempt.StudentIdentifier;
            CompletedAt = attempts.Max(a => a.AttemptDate);

            // Calculate statistics
            var puzzleGroups = attempts.GroupBy(a => a.PuzzleId);
            var totalPuzzles = puzzleGroups.Count();
            var correctPuzzles = puzzleGroups.Count(g => g.Any(a => a.IsCorrect));
            var correctOnFirstTry = puzzleGroups.Count(g => g.First().IsCorrect);

            Statistics = new BundleStatistics
            {
                TotalPuzzles = totalPuzzles,
                CorrectPuzzles = correctPuzzles,
                CorrectOnFirstTry = correctOnFirstTry,
                TotalAttempts = attempts.Count,
                SuccessRate = totalPuzzles > 0 ? (int)((double)correctPuzzles / totalPuzzles * 100) : 0
            };

            // Get detailed results for each puzzle
            PuzzleResults = new List<PuzzleResultViewModel>();
            foreach (var puzzleGroup in puzzleGroups)
            {
                var puzzleAttempts = puzzleGroup.ToList();
                var lastAttempt = puzzleAttempts.Last();

                PuzzleResults.Add(new PuzzleResultViewModel
                {
                    PuzzleId = puzzleGroup.Key,
                    PuzzleTitle = lastAttempt.Puzzle.Title,
                    IsCorrect = puzzleAttempts.Any(a => a.IsCorrect),
                    Attempts = puzzleAttempts.Count,
                    TimeTaken = puzzleAttempts.Sum(a => a.TimeTakenSeconds)
                });
            }

            // Grade passback for LTI sessions
            var sessionIdStr = HttpContext.Session.GetString(LtiSessionKeys.SessionId);
            if (!string.IsNullOrEmpty(sessionIdStr) && int.TryParse(sessionIdStr, out var ltiSessionId))
            {
                var ltiSession = await _context.LtiSessions.FindAsync(ltiSessionId);
                ReturnUrl = ltiSession?.ReturnUrl;

                try
                {
                    await _agsService.SendGradeAsync(ltiSessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Grade passback failed for LTI session {SessionId}", ltiSessionId);
                    // Non-fatal: page still loads even if grade passback fails
                }
            }

            return Page();
        }
    }
}
