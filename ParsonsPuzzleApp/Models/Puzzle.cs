namespace ParsonsPuzzleApp.Models
{
    public class Puzzle
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Task { get; set; }
        public string SourceCode { get; set; }
        public string Distractors { get; set; }
        public Languages Language { get; set; }
        public List<BundlePuzzle> BundlePuzzles { get; set; } = new List<BundlePuzzle>();
        public List<MiniBlock> MiniBlocks { get; set; } = new List<MiniBlock>();

        public List<PuzzleBlock> PuzzleBlocks { get; set; } = new List<PuzzleBlock>();
        public string? BlockConfiguration { get; set; }
    }
}
