using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Services
{
    public class BundleAnalysisService : IBundleAnalysisService
    {
        private readonly ILanguageCategoryService _categoryService;
        private const int PercentageDecimalPlaces = 1;

        public BundleAnalysisService(ILanguageCategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public CollectionLanguageAnalysis AnalyzeLanguages(Bundle bundle)
        {
            var analysis = new CollectionLanguageAnalysis();
            var puzzles = bundle.BundlePuzzles
                .Select(bp => bp.Puzzle)
                .Where(p => p != null && p.Language != null)
                .ToList();

            analysis.TotalPuzzles = puzzles.Count;

            if (analysis.TotalPuzzles == 0)
            {
                return analysis;
            }

            var languageGroups = puzzles
                .GroupBy(p => p.Language!)
                .Select(g => new
                {
                    Language = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            analysis.UniqueLanguages = languageGroups.Count;

            foreach (var group in languageGroups)
            {
                var metadata = _categoryService.GetMetadata(group.Language.Category);

                var languageSummary = new LanguageSummary
                {
                    LanguageId = group.Language.Id,
                    LanguageName = group.Language.Name,
                    DisplayName = group.Language.DisplayName,
                    Category = group.Language.Category,
                    PuzzleCount = group.Count,
                    Percentage = CalculatePercentage(group.Count, analysis.TotalPuzzles),
                    Color = metadata.ForegroundColor,
                    Icon = metadata.Icon
                };

                analysis.LanguageBreakdown.Add(languageSummary);
            }

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

            foreach (var group in categoryGroups)
            {
                var metadata = _categoryService.GetMetadata(group.Category);

                var categorySummary = new CategorySummary
                {
                    Category = group.Category,
                    CategoryName = metadata.DisplayName,
                    PuzzleCount = group.Count,
                    Percentage = CalculatePercentage(group.Count, analysis.TotalPuzzles),
                    Color = metadata.BackgroundColor,
                    Icon = metadata.CategoryIcon,
                    Languages = analysis.LanguageBreakdown
                        .Where(l => l.Category == group.Category)
                        .ToList()
                };

                analysis.CategoryBreakdown.Add(categorySummary);
            }

            return analysis;
        }

        private double CalculatePercentage(int count, int total)
        {
            return Math.Round((double)count / total * 100, PercentageDecimalPlaces);
        }
    }
}
