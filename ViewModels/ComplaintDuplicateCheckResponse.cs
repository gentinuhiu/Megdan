namespace Megdan.Web.Models.ViewModels;

public class ComplaintDuplicateCheckResponse
{
    public bool HasMatches { get; set; }
    public List<ComplaintDuplicateSuggestionViewModel> Matches { get; set; } = new();
}