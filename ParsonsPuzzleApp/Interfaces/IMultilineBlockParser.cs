using ParsonsPuzzleApp.Entities;

namespace ParsonsPuzzleApp.Interfaces
{
    public interface IMultilineBlockParser
    {
        List<PuzzleBlock> ParseSourceCode(string sourceCode, int puzzleId, Language language);
        string GetCommentSyntaxForLanguage(Language language);
    }
}
