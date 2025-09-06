namespace ParsonsPuzzleApp.Models
{
    public class CodeBlock
    {
        public string Content { get; set; }
        public int Indentation { get; set; }
        public string SlotName { get; set; } // null ако няма слот
    }
}
