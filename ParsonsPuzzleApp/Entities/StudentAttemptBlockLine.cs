namespace ParsonsPuzzleApp.Entities
{
    public class StudentAttemptBlockLine
    {
        public int Id { get; set; }
        public int StudentAttemptBlockId { get; set; }
        public StudentAttemptBlock StudentAttemptBlock { get; set; }
        public int PuzzleBlockLineId { get; set; }
        public PuzzleBlockLine PuzzleBlockLine { get; set; }
        public int Position { get; set; }
        public string Content { get; set; }
        public List<StudentAttemptMiniBlock> MiniBlocks { get; set; } = new List<StudentAttemptMiniBlock>();
    }
}
