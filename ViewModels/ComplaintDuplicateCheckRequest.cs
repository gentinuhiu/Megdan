namespace Megdan.Web.Models.ViewModels;

public class ComplaintDuplicateCheckRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}