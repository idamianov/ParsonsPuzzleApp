using ParsonsPuzzleApp.Entities;

namespace ParsonsPuzzleApp.Interfaces
{
    public interface ILanguageIndentationService
    {
        string ProcessIndentation(string code, Languages language);
        bool ValidateIndentation(string studentCode, string correctCode, Languages language);
    }
}