using System.ComponentModel.DataAnnotations;

namespace ParsonsPuzzleApp.Entities
{
    public class Puzzle
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Заглавието е задължително")]
        [StringLength(200, ErrorMessage = "Заглавието не може да бъде по-дълго от 200 символа")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Задачата е задължителна")]
        public string Task { get; set; }

        [Required(ErrorMessage = "Изходният код е задължителен")]
        public string SourceCode { get; set; }

        // Distractors are now optional - no [Required] attribute
        public string? Distractors { get; set; }

        [Required(ErrorMessage = "Езикът е задължителен")]
        public int LanguageId { get; set; }
        
        public Language Language { get; set; } = null!;

        // New property for instructor ownership
        public string InstructorId { get; set; } // Foreign key to AspNetUsers
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedAt { get; set; }

        public ICollection<BundlePuzzle> BundlePuzzles { get; set; } = new List<BundlePuzzle>();
        public ICollection<MiniBlock> MiniBlocks { get; set; } = new List<MiniBlock>();
        public ICollection<PuzzleBlock> PuzzleBlocks { get; set; } = new List<PuzzleBlock>();
        public string? BlockConfiguration { get; set; }
    }
}