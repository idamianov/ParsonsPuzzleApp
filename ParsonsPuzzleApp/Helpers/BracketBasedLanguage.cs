using ParsonsPuzzleApp.Entities;

namespace ParsonsPuzzleApp.Helpers
{
    public static class BracketBasedLanguage
    {
        public static bool IsBracketBasedLanguage(Languages language)
        {
            return language == Languages.C ||
                   language == Languages.Cpp ||
                   language == Languages.CSharp ||
                   language == Languages.Java ||
                   language == Languages.JavaScript;
        }
    }
}
