namespace ParsonsPuzzleApp.Models
{
    public class BundleConfig
    {
        public int BundleId { get; set; }
        public string Identifier { get; set; }
        public List<PuzzleConfig> Puzzles { get; set; }
    }
}
