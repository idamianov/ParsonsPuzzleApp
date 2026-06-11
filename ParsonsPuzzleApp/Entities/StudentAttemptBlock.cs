namespace ParsonsPuzzleApp.Entities
{
    public class StudentAttemptBlock
    {
        public int Id { get; set; }
        public int StudentAttemptId { get; set; }
        public StudentAttempt Attempt { get; set; }
        public int PuzzleBlockId { get; set; }
        public PuzzleBlock PuzzleBlock { get; set; }
        public int Position { get; set; }
        public int Indent {  get; set; }
        public List<StudentAttemptBlockLine> Lines { get; set; } = new List<StudentAttemptBlockLine>();
    }
}
