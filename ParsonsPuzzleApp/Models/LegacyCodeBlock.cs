namespace ParsonsPuzzleApp.Models
{
    public class LegacyCodeBlock
    {
        public string Content { get; set; } = string.Empty;
        public string SlotName { get; set; } = string.Empty;
        public bool IsDistractor { get; set; }
    }
}
