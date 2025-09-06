using ParsonsPuzzleApp.Entities;

namespace ParsonsPuzzleApp.Interfaces
{
    public interface IMultilineBlockParser
    {
        List<PuzzleBlock> ParseSourceCode(string sourceCode, int puzzleId, Languages language);
        string GetCommentSyntaxForLanguage(Languages language);
    }
}
