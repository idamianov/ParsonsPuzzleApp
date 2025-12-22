namespace ParsonsPuzzleApp.Models
{
    public class PuzzleBlockViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string BlockType { get; set; } = string.Empty;
        public bool IsMultiline { get; set; }
        public bool IsOrderIndependent { get; set; }
        public int OrderIndex { get; set; }
        public List<PuzzleBlockLineViewModel> Lines { get; set; } = new List<PuzzleBlockLineViewModel>();
    }
}
