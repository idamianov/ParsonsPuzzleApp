using ParsonsPuzzleApp.Entities;

namespace ParsonsPuzzleApp.Models
{
    public class CollectionLanguageAnalysis
    {
        public int TotalPuzzles { get; set; }
        public int UniqueLanguages { get; set; }
        public int UniqueCategories { get; set; }
        public List<LanguageSummary> LanguageBreakdown { get; set; } = new List<LanguageSummary>();
        public List<CategorySummary> CategoryBreakdown { get; set; } = new List<CategorySummary>();
    }
}
