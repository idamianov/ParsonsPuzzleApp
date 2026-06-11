namespace ParsonsPuzzleApp.Entities
{
    public class MiniBlock
    {
        public int Id { get; set; }
        public int PuzzleBlockLineId { get; set; }
        public string SlotName { get; set; }
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
        public PuzzleBlockLine PuzzleBlockLine { get; set; }
    }
}
