using Megdan.Web.ViewModels.Ai;

namespace Megdan.Web.Services
{
    public interface IAiAnalysisService
    {
        Task<AiComplaintAnalysisResult> AnalyzeComplaintAsync(string title, string description);
        Task<List<int>> FindSimilarComplaintIdsAsync(string title, string description);
    }
}