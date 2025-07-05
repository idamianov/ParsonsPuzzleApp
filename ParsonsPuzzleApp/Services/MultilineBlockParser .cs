using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Services
{
    public interface IMultilineBlockParser
    {
        List<PuzzleBlock> ParseSourceCode(string sourceCode, int puzzleId, Languages language);
        string GetCommentSyntaxForLanguage(Languages language);
    }

    public class MultilineBlockParser : IMultilineBlockParser
    {
        private readonly Dictionary<Languages, string> _commentSyntax = new()
        {
            { Languages.C, "//" },
            { Languages.Cpp, "//" },
            { Languages.CSharp, "//" },
            { Languages.Java, "//" },
            { Languages.JavaScript, "//" },
            { Languages.Python, "#" },
            { Languages.TSQL, "--" },
            { Languages.MySQL, "--" },
            { Languages.PostgreSQL, "--" },
            { Languages.plSQL, "--" }
        };

        public List<PuzzleBlock> ParseSourceCode(string sourceCode, int puzzleId, Languages language)
        {
            var blocks = new List<PuzzleBlock>();
            var lines = sourceCode.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
            var commentSyntax = _commentSyntax[language];

            // Simple patterns: just start and end markers
            var startPattern = $@"^{Regex.Escape(commentSyntax)}-->\s*$";
            var endPattern = $@"^{Regex.Escape(commentSyntax)}<--\s*$";

            int orderIndex = 0;
            int i = 0;

            while (i < lines.Length)
            {
                var line = lines[i];
                var trimmedLine = line.Trim();

                if (Regex.IsMatch(trimmedLine, startPattern))
                {
                    // Found start of multiline block
                    var blockLines = new List<string>();
                    i++; // Skip the start marker

                    // Collect lines until end marker
                    while (i < lines.Length && !Regex.IsMatch(lines[i].Trim(), endPattern))
                    {
                        blockLines.Add(lines[i]);
                        i++;
                    }

                    if (blockLines.Any())
                    {
                        // CRITICAL: Remove indentation hints by trimming leading whitespace
                        var normalizedLines = RemoveIndentationHints(blockLines);
                        var blockContent = string.Join("\n", normalizedLines);
                        var slotName = ExtractSlotName(blockContent);

                        // Create multiline block
                        var block = new PuzzleBlock
                        {
                            PuzzleId = puzzleId,
                            GroupId = $"multiline_{orderIndex}",
                            BlockType = "multiline",
                            IsMultiline = true,
                            IsOrderIndependent = false,
                            OrderIndex = orderIndex++,
                            IsDistractor = false,
                            Content = blockContent,
                            SlotName = slotName
                        };

                        blocks.Add(block);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(trimmedLine) &&
                         !Regex.IsMatch(trimmedLine, endPattern))
                {
                    // Regular single line block - also remove indentation hints
                    var normalizedLine = line.Trim(); // Remove leading/trailing whitespace

                    blocks.Add(new PuzzleBlock
                    {
                        PuzzleId = puzzleId,
                        Content = normalizedLine,
                        BlockType = "single",
                        IsMultiline = false,
                        IsOrderIndependent = false,
                        OrderIndex = orderIndex++,
                        IsDistractor = false,
                        SlotName = ExtractSlotName(normalizedLine)
                    });
                }

                i++;
            }

            return blocks;
        }

        private List<string> RemoveIndentationHints(List<string> lines)
        {
            // Remove all leading whitespace to prevent giving away indentation structure
            var normalizedLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    normalizedLines.Add(trimmedLine);
                }
            }

            return normalizedLines;
        }

        private string? ExtractSlotName(string content)
        {
            var match = Regex.Match(content, @"§(\w+)§");
            return match.Success ? match.Groups[1].Value : null;
        }

        public string GetCommentSyntaxForLanguage(Languages language)
        {
            var syntax = _commentSyntax[language];
            return $@"За {language} използвайте:
{syntax}--> за начало на многоредов блок
{syntax}<-- за край на многоредов блок

Пример:
{syntax}-->
int x = 10;
int y = 20;
{syntax}<--

ВАЖНО: Индентацията от изходния код се премахва автоматично, за да не се дават подсказки на студентите.";
        }
    }

    // Simplified validator
    public static class MultilineBlockValidator
    {
        private static readonly Dictionary<Languages, string> _commentSyntaxMap = new()
        {
            { Languages.C, "//" },
            { Languages.Cpp, "//" },
            { Languages.CSharp, "//" },
            { Languages.Java, "//" },
            { Languages.JavaScript, "//" },
            { Languages.Python, "#" },
            { Languages.TSQL, "--" },
            { Languages.MySQL, "--" },
            { Languages.PostgreSQL, "--" },
            { Languages.plSQL, "--" }
        };

        public static bool ValidateBlockSyntax(string sourceCode, Languages language)
        {
            if (!_commentSyntaxMap.TryGetValue(language, out var commentSyntax))
                return true;

            var startPattern = $@"^{Regex.Escape(commentSyntax)}-->\s*$";
            var endPattern = $@"^{Regex.Escape(commentSyntax)}<--\s*$";

            var lines = sourceCode.Split('\n');
            int openBlocks = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (Regex.IsMatch(trimmed, startPattern))
                    openBlocks++;
                else if (Regex.IsMatch(trimmed, endPattern))
                    openBlocks--;

                if (openBlocks < 0)
                    return false; // Closing tag without opening
            }

            return openBlocks == 0; // All blocks are closed
        }
    }
}