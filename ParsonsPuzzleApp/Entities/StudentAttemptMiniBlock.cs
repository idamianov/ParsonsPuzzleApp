namespace ParsonsPuzzleApp.Entities
{
    public class StudentAttemptMiniBlock
    {
        public int Id { get; set; }
        public int? StudentAttemptBlockLineId { get; set; }
        public StudentAttemptBlockLine AttemptBlockLine { get; set; }
        public int MiniBlockId { get; set; }
        public MiniBlock MiniBlock { get; set; }
        public int Position { get; set; }
    }
}
