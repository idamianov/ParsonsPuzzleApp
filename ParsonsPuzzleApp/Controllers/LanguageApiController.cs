using Microsoft.AspNetCore.Mvc;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;
using ParsonsPuzzleApp.Services;

namespace ParsonsPuzzleApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LanguageApiController : ControllerBase
    {
        private readonly ILanguageService _languageService;

        public LanguageApiController(ILanguageService languageService)
        {
            _languageService = languageService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LanguageDto>>> GetLanguages()
        {
            var languages = await _languageService.GetActiveLanguagesAsync();
            return Ok(languages);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LanguageDto>> GetLanguage(int id)
        {
            var language = await _languageService.GetLanguageByIdAsync(id);
            
            if (language == null)
            {
                return NotFound();
            }

            return Ok(language);
        }
    }
}

