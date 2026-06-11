using ParsonsPuzzleApp.Entities;

namespace ParsonsPuzzleApp.Interfaces
{
    public interface ILanguageIndentationService
    {
        string ProcessIndentation(string code, Language language);
        bool ValidateIndentation(string studentCode, string correctCode, Language language);
    }
}