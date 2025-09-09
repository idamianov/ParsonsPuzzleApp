namespace ParsonsPuzzleApp.Models
{
    public class SubmitSolutionModel
    {
        public int BundleId { get; set; }
        public int PuzzleId { get; set; }
        public int PuzzleIndex { get; set; }
        public string StudentIdentifier { get; set; } = string.Empty;
        public string Arrangement { get; set; } = string.Empty;
        public int TimeTaken { get; set; }
        public Guid BundleAttemptId { get; set; }
    }
}
