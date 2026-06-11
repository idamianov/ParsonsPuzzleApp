using ParsonsPuzzleApp.Entities;

namespace ParsonsPuzzleApp.Models
{
    public class PuzzleConfig
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Task { get; set; }
        public Language Language { get; set; }
        public List<CodeBlock> CodeBlocks { get; set; }
        public List<MiniBlockConfig> MiniBlocks { get; set; }
    }
}
