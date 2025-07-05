namespace ParsonsPuzzleApp.Services
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Web;
    using ParsonsPuzzleApp.Models;

    public class LanguageIndentationService : ILanguageIndentationService
    {
        public string ProcessIndentation(string code, Languages language)
        {
            // For bracket-based languages, add braces when indentation changes
            if (IsBracketBasedLanguage(language))
            {
                return AddBracesForIndentation(code);
            }

            // For other languages, return as-is
            return code;
        }

        public bool ValidateIndentation(string studentCode, string correctCode, Languages language)
        {
            // Clean and normalize both codes
            var normalizedStudent = CleanAndNormalizeCode(studentCode, language);
            var normalizedCorrect = CleanAndNormalizeCode(correctCode, language);

            if (language == Languages.Python)
            {
                // For Python, indentation matters - compare with preserved indentation
                return CompareWithIndentation(normalizedStudent, normalizedCorrect);
            }
            else if (IsBracketBasedLanguage(language))
            {
                // For bracket-based languages, ignore indentation but keep structure
                return CompareIgnoringIndentation(normalizedStudent, normalizedCorrect);
            }
            else
            {
                // For SQL and other languages, compare ignoring indentation
                return CompareIgnoringIndentation(normalizedStudent, normalizedCorrect);
            }
        }

        private bool IsBracketBasedLanguage(Languages language)
        {
            return language == Languages.C ||
                   language == Languages.Cpp ||
                   language == Languages.CSharp ||
                   language == Languages.Java ||
                   language == Languages.JavaScript;
        }

        private string CleanAndNormalizeCode(string code, Languages language)
        {
            if (string.IsNullOrWhiteSpace(code))
                return string.Empty;

            // Decode HTML entities that might have been encoded
            code = HttpUtility.HtmlDecode(code);

            // Split into lines and clean each
            var lines = code.Split('\n')
                .Select(l => l.TrimEnd('\r', '\n', ' ', '\t'))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            // Remove comment markers for multiline blocks
            var commentSyntax = GetCommentSyntax(language);
            var cleanedLines = lines
                .Where(l => !IsCommentMarker(l.Trim(), commentSyntax))
                .ToList();

            return string.Join("\n", cleanedLines);
        }

        private bool IsCommentMarker(string line, string commentSyntax)
        {
            // Check for simplified multiline block markers like //-->, //<--, etc.
            var startPattern = $@"^{Regex.Escape(commentSyntax)}-->\s*$";
            var endPattern = $@"^{Regex.Escape(commentSyntax)}<--?\s*$"; // Handle both <-- and <-

            return Regex.IsMatch(line, startPattern) || Regex.IsMatch(line, endPattern);
        }

        private string GetCommentSyntax(Languages language)
        {
            return language switch
            {
                Languages.C or Languages.Cpp or Languages.CSharp or Languages.Java or Languages.JavaScript => "//",
                Languages.Python => "#",
                Languages.TSQL or Languages.MySQL or Languages.PostgreSQL or Languages.plSQL => "--",
                _ => "//"
            };
        }

        private bool CompareWithIndentation(string studentCode, string correctCode)
        {
            // For Python - preserve indentation and compare line by line
            var studentLines = studentCode.Split('\n')
                .Select(l => l.TrimEnd())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            var correctLines = correctCode.Split('\n')
                .Select(l => l.TrimEnd())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            if (studentLines.Length != correctLines.Length)
                return false;

            for (int i = 0; i < studentLines.Length; i++)
            {
                // For Python, both content and indentation must match
                if (!string.Equals(studentLines[i], correctLines[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareIgnoringIndentation(string studentCode, string correctCode)
        {
            // For bracket-based and SQL languages - ignore indentation, compare content only
            var studentLines = studentCode.Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            var correctLines = correctCode.Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            if (studentLines.Length != correctLines.Length)
                return false;

            // Sort both arrays for comparison since order might not matter for some constructs
            Array.Sort(studentLines);
            Array.Sort(correctLines);

            for (int i = 0; i < studentLines.Length; i++)
            {
                if (!string.Equals(studentLines[i], correctLines[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private string AddBracesForIndentation(string code)
        {
            // This method adds braces based on indentation changes
            // Used when student adds indentation via UI buttons
            var lines = code.Split('\n').ToList();
            var result = new List<string>();
            var braceStack = new Stack<int>();

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var currentIndent = GetIndentationLevel(line);

                if (i > 0)
                {
                    var prevIndent = GetIndentationLevel(lines[i - 1]);

                    // Close braces if indentation decreased
                    while (braceStack.Count > 0 && braceStack.Peek() >= currentIndent)
                    {
                        var braceIndent = braceStack.Pop();
                        result.Add(new string(' ', braceIndent * 2) + "}");
                    }

                    // Open brace if indentation increased
                    if (currentIndent > prevIndent)
                    {
                        result.Add(new string(' ', prevIndent * 2) + "{");
                        braceStack.Push(currentIndent);
                    }
                }
                else if (currentIndent > 0)
                {
                    // First line with indentation
                    result.Add("{");
                    braceStack.Push(currentIndent);
                }

                result.Add(line);
            }

            // Close any remaining open braces
            while (braceStack.Count > 0)
            {
                var braceIndent = braceStack.Pop();
                result.Add(new string(' ', braceIndent * 2) + "}");
            }

            return string.Join("\n", result);
        }

        private int GetIndentationLevel(string line)
        {
            int spaces = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ' ')
                    spaces++;
                else if (line[i] == '\t')
                    spaces += 4; // Assume 4 spaces per tab
                else
                    break;
            }
            return spaces / 2; // Assuming 2 spaces per indentation level
        }
    }
}