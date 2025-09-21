using ParsonsPuzzleApp.Entities;

namespace ParsonsPuzzleApp.Models
{
    public class LanguageSummary
    {
        public int LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public LanguageCategory Category { get; set; }
        public int PuzzleCount { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}
