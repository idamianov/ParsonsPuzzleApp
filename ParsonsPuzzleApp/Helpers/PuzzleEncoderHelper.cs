using ParsonsPuzzleApp.Entities;
using System.Text;
using System.Text.RegularExpressions;

namespace ParsonsPuzzleApp.Helpers
{
    public static class PuzzleEncoderHelper
    {
        public static (Dictionary<int, char> lineMap, Dictionary<string, char> slotMap) BuildLetterMaps(Puzzle puzzle)
        {
            var lineMap = new Dictionary<int, char>();
            var slotMap = new Dictionary<string, char>();

            char current = 'a';

            foreach (var block in puzzle.PuzzleBlocks.OrderBy(b => b.OrderIndex))
            {
                foreach (var line in block.Lines)
                {
                    lineMap[line.Id] = current++;

                    foreach (var slot in line.MiniBlocks.Where(m => m.IsCorrect))
                    {
                        var key = slot.SlotName + "|" + slot.Content;

                        slotMap[key] = current++;
                    }
                }
            }

            return new(lineMap, slotMap);
        }

        public static string EncodeCorrectSolution(Puzzle puzzle, (Dictionary<int, char> lineMap, Dictionary<string, char> slotMap) maps)
        {
            var sb = new StringBuilder();

            foreach (var block in puzzle.PuzzleBlocks.OrderBy(b => b.OrderIndex))
            {
                foreach (var line in block.Lines.Where(l => !l.IsDistractor))
                {
                    char lineLetter = maps.lineMap[line.Id];

                    var slots = line.MiniBlocks.Where(m => m.IsCorrect).ToList();

                    if (slots.Any())
                    {
                        sb.Append("(");
                        sb.Append(lineLetter);

                        var orderedSlots = OrderSlotsByTemplateAppearance(slots, line.Content);

                        foreach (var s in orderedSlots)
                        {
                            var key = s.SlotName + "|" + s.Content;
                            sb.Append(maps.slotMap[key]);
                        }

                        sb.Append(")");
                    }
                    else
                    {
                        sb.Append(lineLetter);
                    }

                    sb.Append(block.Indent);
                }
            }

            return sb.ToString();
        }

        private static List<MiniBlock> OrderSlotsByTemplateAppearance(List<MiniBlock> slots, string lineContent)
        {
            var slotPattern = new Regex(@"§([^§]+)§");
            var matches = slotPattern.Matches(lineContent);

            var slotOrder = new List<string>();
            foreach (Match match in matches)
            {
                slotOrder.Add(match.Groups[1].Value);
            }

            return slots.OrderBy(slot =>
            {
                var index = slotOrder.IndexOf(slot.SlotName);
                return index == -1 ? int.MaxValue : index; // Put unknown slots at the end
            }).ToList();
        }
    }
}
