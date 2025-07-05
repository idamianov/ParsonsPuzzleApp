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
        private readonly Dictionary<Languages, CommentSyntax> _commentSyntax = new()
        {
            { Languages.C, new CommentSyntax { Single = "//", MultiStart = "/*", MultiEnd = "*/" } },
            { Languages.Cpp, new CommentSyntax { Single = "//", MultiStart = "/*", MultiEnd = "*/" } },
            { Languages.CSharp, new CommentSyntax { Single = "//", MultiStart = "/*", MultiEnd = "*/" } },
            { Languages.Java, new CommentSyntax { Single = "//", MultiStart = "/*", MultiEnd = "*/" } },
            { Languages.JavaScript, new CommentSyntax { Single = "//", MultiStart = "/*", MultiEnd = "*/" } },
            { Languages.Python, new CommentSyntax { Single = "#" } },
            { Languages.TSQL, new CommentSyntax { Single = "--", MultiStart = "/*", MultiEnd = "*/" } },
            { Languages.MySQL, new CommentSyntax { Single = "--", MultiStart = "/*", MultiEnd = "*/" } },
            { Languages.PostgreSQL, new CommentSyntax { Single = "--", MultiStart = "/*", MultiEnd = "*/" } },
            { Languages.plSQL, new CommentSyntax { Single = "--", MultiStart = "/*", MultiEnd = "*/" } }
        };

        public List<PuzzleBlock> ParseSourceCode(string sourceCode, int puzzleId, Languages language)
        {
            var blocks = new List<PuzzleBlock>();
            var lines = sourceCode.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
            var syntax = _commentSyntax[language];

            var startPattern = $@"^{Regex.Escape(syntax.Single)}-->\[(\w+):(ordered|unordered)\]";
            var endPattern = $@"^{Regex.Escape(syntax.Single)}<--";

            int orderIndex = 0;
            int i = 0;

            while (i < lines.Length)
            {
                var line = lines[i];
                var startMatch = Regex.Match(line.Trim(), startPattern);

                if (startMatch.Success)
                {
                    // Намерили сме начало на многоредов блок
                    var blockName = startMatch.Groups[1].Value;
                    var ordering = startMatch.Groups[2].Value;
                    var blockLines = new List<string>();

                    i++; // Прескачаме началния маркер

                    // Събираме редовете до края на блока
                    while (i < lines.Length && !Regex.IsMatch(lines[i].Trim(), endPattern))
                    {
                        blockLines.Add(lines[i]);
                        i++;
                    }

                    if (blockLines.Any())
                    {
                        // Проверяваме дали блокът съдържа слотове
                        var blockContent = string.Join("\n", blockLines);
                        var slotName = ExtractSlotName(blockContent);

                        // Създаваме многоредов блок
                        var block = new PuzzleBlock
                        {
                            PuzzleId = puzzleId,
                            GroupId = blockName,
                            BlockType = blockName.ToLower(),
                            IsMultiline = true,
                            IsOrderIndependent = ordering == "unordered",
                            OrderIndex = orderIndex++,
                            IsDistractor = false,
                            Content = blockContent,
                            SlotName = slotName
                        };

                        // НЕ добавяме Lines тук - ще ги добавим в CreatePuzzle.cshtml.cs
                        blocks.Add(block);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(line) && !Regex.IsMatch(line.Trim(), endPattern))
                {
                    // Обикновен единичен блок
                    blocks.Add(new PuzzleBlock
                    {
                        PuzzleId = puzzleId,
                        Content = line,
                        BlockType = "single",
                        IsMultiline = false,
                        IsOrderIndependent = false,
                        OrderIndex = orderIndex++,
                        IsDistractor = false,
                        SlotName = ExtractSlotName(line)
                    });
                }

                i++;
            }

            return blocks;
        }

        // Нов метод за получаване на линиите за блок
        public List<string> GetBlockLines(string blockContent)
        {
            return blockContent.Split('\n')
                .Select(l => l.TrimEnd('\r'))
                .ToList();
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
{syntax.Single}-->[име:ordered] за подреден блок
{syntax.Single}-->[име:unordered] за неподреден блок
{syntax.Single}<-- за край на блок

Пример:
{syntax.Single}-->[variables:unordered]
int x = 10;
int y = 20;
{syntax.Single}<--";
        }

        private class CommentSyntax
        {
            public string Single { get; set; }
            public string MultiStart { get; set; }
            public string MultiEnd { get; set; }
        }
    }

    // Статичен помощен клас за валидация
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
                return true; // Ако не познаваме езика, приемаме че е валиден

            var startPattern = $@"^{Regex.Escape(commentSyntax)}-->\[(\w+):(ordered|unordered)\]";
            var endPattern = $@"^{Regex.Escape(commentSyntax)}<--";

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
                    return false; // Затварящ таг без отварящ
            }

            return openBlocks == 0; // Всички блокове са затворени
        }
    }
}