using ParsonsPuzzleApp.Entities;

namespace ParsonsPuzzleApp.Interfaces
{
    public interface IPuzzleBlockService
    {
        Task<List<PuzzleBlock>> GetPuzzleBlocksAsync(int puzzleId);
        Task<PuzzleBlock> CreatePuzzleBlockAsync(PuzzleBlock block);
        Task UpdatePuzzleBlockAsync(PuzzleBlock block);
        Task DeletePuzzleBlockAsync(int blockId);
        string GenerateBlockConfiguration(List<PuzzleBlock> blocks);
    }
}