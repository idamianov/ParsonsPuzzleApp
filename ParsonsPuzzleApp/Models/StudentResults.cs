namespace ParsonsPuzzleApp.Models
{
    public class StudentResults
    {
        public string StudentIdentifier { get; set; }
        public List<AttemptSummary> Attempts { get; set; }
    }
}
