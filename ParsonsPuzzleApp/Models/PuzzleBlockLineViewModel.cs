namespace ParsonsPuzzleApp.Models
{
    public class PuzzleBlockLineViewModel
    {
        public string Content { get; set; }
        public int LineOrder { get; set; }
        public bool IsOptional { get; set; }
        public bool IsDistractor { get; set; }
        public List<string> Slots { get; set; } = new List<string>();
    }
}
