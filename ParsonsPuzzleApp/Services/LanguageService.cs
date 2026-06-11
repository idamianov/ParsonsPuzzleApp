using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Services
{
    public class LanguageService : ILanguageService
    {
        private readonly ApplicationDbContext _context;

        public LanguageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LanguageDto>> GetActiveLanguagesAsync()
        {
            return await _context.Languages
                .Where(l => l.IsActive)
                .OrderBy(l => l.SortOrder)
                .Select(l => new LanguageDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    DisplayName = l.DisplayName,
                    Category = l.Category.ToString(),
                    CommentSyntax = l.CommentSyntax,
                    CodeMirrorMode = l.CodeMirrorMode,
                    IsBracketBased = l.IsBracketBased,
                    IsIndentationSensitive = l.IsIndentationSensitive,
                    IsSqlBased = l.IsSqlBased
                })
                .ToListAsync();
        }

        public async Task<LanguageDto?> GetLanguageByIdAsync(int id)
        {
            return await _context.Languages
                .Where(l => l.Id == id && l.IsActive)
                .Select(l => new LanguageDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    DisplayName = l.DisplayName,
                    Category = l.Category.ToString(),
                    CommentSyntax = l.CommentSyntax,
                    CodeMirrorMode = l.CodeMirrorMode,
                    IsBracketBased = l.IsBracketBased,
                    IsIndentationSensitive = l.IsIndentationSensitive,
                    IsSqlBased = l.IsSqlBased
                })
                .FirstOrDefaultAsync();
        }
    }
}
