namespace ParsonsPuzzleApp.Models
{
    public class PuzzleResultViewModel
    {
        public int PuzzleId { get; set; }
        public string PuzzleTitle { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int Attempts { get; set; }
        public int TimeTaken { get; set; }
    }
}
