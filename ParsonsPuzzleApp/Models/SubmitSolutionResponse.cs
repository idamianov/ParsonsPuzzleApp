namespace ParsonsPuzzleApp.Models
{
    public class SubmitSolutionResponse
    {
        public bool IsLastPuzzle { get; set; }
        public bool IsCorrect { get; set; }
        public SolutionStatisticsModel? Statistics { get; set; }
        public string NextUrl { get; set; } = string.Empty;
    }
}
