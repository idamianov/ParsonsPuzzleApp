using ParsonsPuzzleApp.Entities;

namespace ParsonsPuzzleApp.Models
{
    public class CategorySummary
    {
        public LanguageCategory Category { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int PuzzleCount { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public List<LanguageSummary> Languages { get; set; } = new List<LanguageSummary>();
    }
}
