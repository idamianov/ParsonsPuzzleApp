using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Interfaces
{
    public interface ILanguageCategoryService
    {
        LanguageCategoryMetadata GetMetadata(LanguageCategory category);
    }
}
