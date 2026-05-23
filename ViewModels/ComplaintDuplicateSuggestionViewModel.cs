namespace Megdan.Web.Models.ViewModels;

public class ComplaintDuplicateSuggestionViewModel
{
    public int ComplaintId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string LocationLabel { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public int SimilarityPercent { get; set; }
    public string MatchReason { get; set; } = string.Empty;
}