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
}
