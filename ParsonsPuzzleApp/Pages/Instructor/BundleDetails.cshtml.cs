using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Pages.Instructor
{
    [Authorize]
    public class BundleDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public BundleDetailsModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Bundle Bundle { get; set; }
        public string ShareableUrl { get; set; }
        public CollectionLanguageAnalysis LanguageAnalysis { get; set; } = new CollectionLanguageAnalysis();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            Bundle = await _context.Bundles
                .Include(b => b.BundlePuzzles)
                .ThenInclude(bp => bp.Puzzle)
                .ThenInclude(p => p.Language)
                .FirstOrDefaultAsync(b => b.Id == id && b.InstructorId == userId);

            if (Bundle == null)
            {
                return NotFound();
            }

            // Generate the full shareable URL
            var request = HttpContext.Request;
            ShareableUrl = $"{request.Scheme}://{request.Host}/bundle/{Bundle.ShareableLink}";

            // Analyze languages in the collection
            LanguageAnalysis = AnalyzeLanguages(Bundle);

            return Page();
        }

        public async Task<IActionResult> OnPostPublishAsync(int id)
        {
            var userId = _userManager.GetUserId(User);
            var bundle = await _context.Bundles
                .FirstOrDefaultAsync(b => b.Id == id && b.InstructorId == userId);

            if (bundle == null)
            {
                return NotFound();
            }

            bundle.IsPublished = true;
            bundle.LastModifiedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Колекцията беше успешно публикувана!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Грешка при публикуване на колекцията.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostUnpublishAsync(int id)
        {
            var userId = _userManager.GetUserId(User);
            var bundle = await _context.Bundles
                .FirstOrDefaultAsync(b => b.Id == id && b.InstructorId == userId);

            if (bundle == null)
            {
                return NotFound();
            }

            bundle.IsPublished = false;
            bundle.LastModifiedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Колекцията беше успешно скрита!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Грешка при скриване на колекцията.";
            }

            return RedirectToPage(new { id });
        }

        private CollectionLanguageAnalysis AnalyzeLanguages(Bundle bundle)
        {
            var analysis = new CollectionLanguageAnalysis();
            var puzzles = bundle.BundlePuzzles.Select(bp => bp.Puzzle).ToList();
            
            analysis.TotalPuzzles = puzzles.Count;
            
            if (analysis.TotalPuzzles == 0)
            {
                return analysis;
            }

            // Group puzzles by language
            var languageGroups = puzzles
                .GroupBy(p => p.Language)
                .Select(g => new
                {
                    Language = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            analysis.UniqueLanguages = languageGroups.Count;

            // Create language summaries
            foreach (var group in languageGroups)
            {
                var languageSummary = new LanguageSummary
                {
                    LanguageId = group.Language.Id,
                    LanguageName = group.Language.Name,
                    DisplayName = group.Language.DisplayName,
                    Category = group.Language.Category,
                    PuzzleCount = group.Count,
                    Percentage = Math.Round((double)group.Count / analysis.TotalPuzzles * 100, 1),
                    Color = GetLanguageColor(group.Language.Category),
                    Icon = GetLanguageIcon(group.Language.Category)
                };
                
                analysis.LanguageBreakdown.Add(languageSummary);
            }

            // Group by category
            var categoryGroups = languageGroups
                .GroupBy(x => x.Language.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Sum(x => x.Count),
                    Languages = g.Select(x => x.Language).ToList()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            analysis.UniqueCategories = categoryGroups.Count;

            // Create category summaries
            foreach (var group in categoryGroups)
            {
                var categorySummary = new CategorySummary
                {
                    Category = group.Category,
                    CategoryName = GetCategoryDisplayName(group.Category),
                    PuzzleCount = group.Count,
                    Percentage = Math.Round((double)group.Count / analysis.TotalPuzzles * 100, 1),
                    Color = GetCategoryColor(group.Category),
                    Icon = GetCategoryIcon(group.Category),
                    Languages = analysis.LanguageBreakdown
                        .Where(l => l.Category == group.Category)
                        .ToList()
                };
                
                analysis.CategoryBreakdown.Add(categorySummary);
            }

            return analysis;
        }

        private string GetLanguageColor(LanguageCategory category)
        {
            return category switch
            {
                LanguageCategory.Bracket => "#007bff",
                LanguageCategory.Indentation => "#28a745",
                LanguageCategory.SQL => "#ffc107",
                _ => "#6c757d"
            };
        }

        private string GetLanguageIcon(LanguageCategory category)
        {
            return category switch
            {
                LanguageCategory.Bracket => "fas fa-code",
                LanguageCategory.Indentation => "fab fa-python",
                LanguageCategory.SQL => "fas fa-database",
                _ => "fas fa-file-code"
            };
        }

        private string GetCategoryColor(LanguageCategory category)
        {
            return category switch
            {
                LanguageCategory.Bracket => "#e3f2fd",
                LanguageCategory.Indentation => "#e8f5e8",
                LanguageCategory.SQL => "#fff8e1",
                _ => "#f5f5f5"
            };
        }

        private string GetCategoryIcon(LanguageCategory category)
        {
            return category switch
            {
                LanguageCategory.Bracket => "fas fa-braces",
                LanguageCategory.Indentation => "fas fa-indent",
                LanguageCategory.SQL => "fas fa-table",
                _ => "fas fa-layer-group"
            };
        }

        private string GetCategoryDisplayName(LanguageCategory category)
        {
            return category switch
            {
                LanguageCategory.Bracket => "Bracket-based Languages",
                LanguageCategory.Indentation => "Indentation-sensitive Languages",
                LanguageCategory.SQL => "SQL-based Languages",
                _ => "Other Languages"
            };
        }
    }
}