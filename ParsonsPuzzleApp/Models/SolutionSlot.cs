using System.Text.Json.Serialization;

namespace ParsonsPuzzleApp.Models
{
    public class SolutionSlot
    {
        [JsonPropertyName("slotId")]
        public string SlotName { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("miniBlockId")]
        public int? MiniBlockId { get; set; }
    }
}
