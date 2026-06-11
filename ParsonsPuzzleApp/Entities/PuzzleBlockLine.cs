namespace ParsonsPuzzleApp.Entities
{
    public class PuzzleBlockLine
    {
        public int Id { get; set; }
        public int PuzzleBlockId { get; set; }
        public string Content { get; set; }
        public bool IsDistractor { get; set; }
        public int LineOrder { get; set; }
        public bool IsOptional { get; set; }
        public PuzzleBlock PuzzleBlock { get; set; }
        public List<MiniBlock> MiniBlocks { get; set; } = new List<MiniBlock>();
    }
}
