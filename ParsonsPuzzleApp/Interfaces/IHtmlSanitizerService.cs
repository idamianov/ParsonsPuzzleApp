namespace ParsonsPuzzleApp.Interfaces
{
    public interface IHtmlSanitizerService
    {
        string SanitizeHtml(string html);
        bool ContainsDangerousContent(string html);
    }
}
