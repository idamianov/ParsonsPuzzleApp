using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Interfaces
{
    public interface IPuzzleSolutionService
    {
        Task<int> CheckSolutionAsync(CheckRequestModel model);
        Task<SubmitSolutionResponse> SubmitSolutionAsync(SubmitSolutionModel model);
    }
}
