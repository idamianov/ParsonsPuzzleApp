using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;

namespace ParsonsPuzzleApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LanguageApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LanguageApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LanguageDto>>> GetLanguages()
        {
            var languages = await _context.Languages
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

            return Ok(languages);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LanguageDto>> GetLanguage(int id)
        {
            var language = await _context.Languages
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

            if (language == null)
            {
                return NotFound();
            }

            return Ok(language);
        }
    }

    public class LanguageDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string CommentSyntax { get; set; } = string.Empty;
        public string CodeMirrorMode { get; set; } = string.Empty;
        public bool IsBracketBased { get; set; }
        public bool IsIndentationSensitive { get; set; }
        public bool IsSqlBased { get; set; }
    }
}

