namespace ParsonsPuzzleApp.Models
{
    public class LanguageDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string CommentSyntax { get; set; } = string.Empty;
        public string CodeMirrorMode { get; set; } = string.Empty;
        public bool IsBracketBased { get; set; }
        public bool IsIndentationSensitive { get; set; }
        public bool IsSqlBased { get; set; }
    }
}
