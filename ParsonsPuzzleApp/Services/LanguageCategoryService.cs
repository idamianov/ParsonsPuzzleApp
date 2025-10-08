using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Services
{
    public class LanguageCategoryService : ILanguageCategoryService
    {
        private static readonly Dictionary<LanguageCategory, LanguageCategoryMetadata> _metadata = new()
        {
            [LanguageCategory.Bracket] = new()
            {
                DisplayName = "Bracket-based Languages",
                ForegroundColor = "#007bff",
                BackgroundColor = "#e3f2fd",
                Icon = "fas fa-code",
                CategoryIcon = "fas fa-braces"
            },
            [LanguageCategory.Indentation] = new()
            {
                DisplayName = "Indentation-sensitive Languages",
                ForegroundColor = "#28a745",
                BackgroundColor = "#e8f5e8",
                Icon = "fab fa-python",
                CategoryIcon = "fas fa-indent"
            },
            [LanguageCategory.SQL] = new()
            {
                DisplayName = "SQL-based Languages",
                ForegroundColor = "#ffc107",
                BackgroundColor = "#fff8e1",
                Icon = "fas fa-database",
                CategoryIcon = "fas fa-table"
            }
        };

        private static readonly LanguageCategoryMetadata _defaultMetadata = new()
        {
            DisplayName = "Other Languages",
            ForegroundColor = "#6c757d",
            BackgroundColor = "#f5f5f5",
            Icon = "fas fa-file-code",
            CategoryIcon = "fas fa-layer-group"
        };

        public LanguageCategoryMetadata GetMetadata(LanguageCategory category)
        {
            return _metadata.TryGetValue(category, out var metadata)
                ? metadata
                : _defaultMetadata;
        }
    }
}
