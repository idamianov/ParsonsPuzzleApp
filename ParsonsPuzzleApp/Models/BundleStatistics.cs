namespace ParsonsPuzzleApp.Models
{
    public class BundleStatistics
    {
        public int TotalPuzzles { get; set; }
        public int CorrectPuzzles { get; set; }
        public int CorrectOnFirstTry { get; set; }
        public int TotalAttempts { get; set; }
        public int SuccessRate { get; set; }
    }
}
