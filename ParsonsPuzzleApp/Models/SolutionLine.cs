using System.Text.Json.Serialization;

namespace ParsonsPuzzleApp.Models
{
    public class SolutionLine
    {
        [JsonPropertyName("lineIndex")]
        public int LineIndex { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("slots")]
        public List<SolutionSlot> Slots { get; set; } = new();
    }
}
