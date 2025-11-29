using Microsoft.AspNetCore.Mvc;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PuzzleApiController : ControllerBase
    {
        private readonly IPuzzleSolutionService _puzzleSolutionService;

        public PuzzleApiController(IPuzzleSolutionService puzzleSolutionService)
        {
            _puzzleSolutionService = puzzleSolutionService;
        }

        [HttpPost("check")]
        public async Task<IActionResult> CheckSolution([FromBody] CheckRequestModel model)
        {
            if (model == null || model.Arrangement == null || model.PuzzleId <= 0)
            {
                return BadRequest("Invalid request data.");
            }

            var result = await _puzzleSolutionService.CheckSolutionAsync(model);

            return Ok(result);
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitSolution([FromBody] SubmitSolutionModel model)
        {
            if (model == null || 
                model.Arrangement == null || 
                model.PuzzleId <= 0 ||
                model.BundleId <= 0 || 
                string.IsNullOrEmpty(model.StudentIdentifier) || 
                model.BundleAttemptId == Guid.Empty)
            {
                return BadRequest("Invalid request data.");
            }

            var result = await _puzzleSolutionService.SubmitSolutionAsync(model);

            return Ok(result);
        }
    }
}