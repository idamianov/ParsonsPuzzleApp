using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Interfaces
{
    public interface IPuzzleSolutionService
    {
        Task<bool> CheckSolutionAsync(CheckRequestModel model);
        Task<SubmitSolutionResponse> SubmitSolutionAsync(SubmitSolutionModel model);
    }
}
