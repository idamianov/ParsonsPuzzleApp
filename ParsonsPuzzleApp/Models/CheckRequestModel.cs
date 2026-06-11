namespace ParsonsPuzzleApp.Models
{
    public class CheckRequestModel
    {
        public int PuzzleId { get; set; }
        public List<ArrangementModel> Arrangement { get; set; } = new List<ArrangementModel>();
    }
}
