namespace Megdan.Web.ViewModels.Ai
{
    public class AiComplaintAnalysisResult
    {
        public string Category { get; set; } = "Uncategorized";
        public string Priority { get; set; } = "Medium";
        public string FrustrationLevel { get; set; } = "Low";
        public string Summary { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}