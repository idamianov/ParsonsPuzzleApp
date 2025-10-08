using System.ComponentModel.DataAnnotations;

namespace ParsonsPuzzleApp.Entities
{
    public class Language
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        public LanguageCategory Category { get; set; }

        [Required]
        [StringLength(10)]
        public string CommentSyntax { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string CodeMirrorMode { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }

        // Navigation properties
        public ICollection<Puzzle> Puzzles { get; set; } = new List<Puzzle>();

        // Business logic helpers
        public bool IsBracketBased => Category == LanguageCategory.Bracket;
        
        public bool IsIndentationSensitive => Category == LanguageCategory.Indentation;
        
        public bool IsSqlBased => Category == LanguageCategory.SQL;

        public string GetMultilineStartMarker() => $"{CommentSyntax}-->";
        
        public string GetMultilineEndMarker() => $"{CommentSyntax}<--";
    }
}
