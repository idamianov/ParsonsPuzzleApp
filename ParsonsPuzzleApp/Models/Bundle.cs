namespace ParsonsPuzzleApp.Models
{
    public class Bundle
    {
        public int Id { get; set; }
        public string Identifier { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
        public List<BundlePuzzle> BundlePuzzles { get; set; } = new List<BundlePuzzle>();
    }
}
