namespace Megdan.Web.ViewModels
{
    public class UserComplaintItemViewModel
    {
        public int ComplaintId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string? Category { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string? Address { get; set; }
        public string TrackingToken { get; set; } = string.Empty;
    }
}