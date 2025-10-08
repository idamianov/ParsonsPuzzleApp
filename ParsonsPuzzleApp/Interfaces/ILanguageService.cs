using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Interfaces
{
    public interface ILanguageService
    {
        Task<IEnumerable<LanguageDto>> GetActiveLanguagesAsync();
        Task<LanguageDto?> GetLanguageByIdAsync(int id);
    }
}
