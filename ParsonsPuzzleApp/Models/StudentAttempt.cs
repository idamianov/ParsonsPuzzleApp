namespace ParsonsPuzzleApp.Models
{
    public class StudentAttempt
    {
        public int Id { get; set; }
        public int BundleId { get; set; }
        public Bundle Bundle { get; set; }
        public int PuzzleId { get; set; }
        public Puzzle Puzzle { get; set; }
        public string StudentIdentifier { get; set; } // Име или ID на студента
        public bool IsCorrect { get; set; } // Вярно/невярно
        public string StudentArrangement { get; set; } // JSON с подредбата
        public int TimeTakenSeconds { get; set; } // Време за решаване
        public DateTime AttemptDate { get; set; } = DateTime.UtcNow;

        public Guid BundleAttemptId { get; set; } // Добавено за групиране на опити
    }
}
