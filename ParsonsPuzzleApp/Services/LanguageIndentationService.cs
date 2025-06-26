namespace ParsonsPuzzleApp.Services
{
    using System;
    using ParsonsPuzzleApp.Models;

    public class LanguageIndentationService : ILanguageIndentationService
    {
        public string ProcessIndentation(string code, Languages language)
        {
            // За момента връщаме кода както е
            // По-късно можете да добавите специфична логика за различните езици
            return code;
        }

        public bool ValidateIndentation(string studentCode, string correctCode, Languages language)
        {
            // Базова валидация - сравнява нормализираните версии
            var normalizedStudent = NormalizeCode(studentCode, language);
            var normalizedCorrect = NormalizeCode(correctCode, language);

            return string.Equals(normalizedStudent, normalizedCorrect, StringComparison.OrdinalIgnoreCase);
        }

        private string NormalizeCode(string code, Languages language)
        {
            if (language == Languages.Python)
            {
                // За Python запазваме индентацията
                return string.Join("\n", code.Split('\n')
                    .Select(l => l.TrimEnd())
                    .Where(l => !string.IsNullOrWhiteSpace(l)));
            }
            else
            {
                // За останалите езици премахваме водещите интервали
                return string.Join("\n", code.Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l)));
            }
        }
    }
}