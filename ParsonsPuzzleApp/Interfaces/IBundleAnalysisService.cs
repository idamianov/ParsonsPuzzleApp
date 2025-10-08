using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Interfaces
{
    public interface IBundleAnalysisService
    {
        CollectionLanguageAnalysis AnalyzeLanguages(Bundle bundle);
    }
}
