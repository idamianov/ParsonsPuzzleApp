using System.Text.Json.Serialization;

namespace ParsonsPuzzleApp.Models
{
    public class ArrangementModel
    {
        [JsonPropertyName("blockId")]
        public int Id { get; set; }

        [JsonPropertyName("indent")]
        public int Indent { get; set; }

        [JsonPropertyName("lines")]
        public List<SolutionLine> Lines { get; set; } = new();
    }

    public class SolutionLine
    {
        [JsonPropertyName("lineIndex")]
        public int LineIndex { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("slots")]
        public List<SolutionSlot> Slots { get; set; } = new();
    }

    public class SolutionSlot
    {
        [JsonPropertyName("slotId")]
        public string SlotName { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}
