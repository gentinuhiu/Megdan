namespace Megdan.Web.Services;

public class ComplaintSimilarityResult
{
    public int ComplaintId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string LocationLabel { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public double SimilarityScore { get; set; }
    public string MatchReason { get; set; } = string.Empty;
}