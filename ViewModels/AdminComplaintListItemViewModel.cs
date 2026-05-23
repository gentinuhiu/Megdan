using Megdan.Web.Models.Enums;

namespace Megdan.Web.ViewModels
{
    public class AdminComplaintListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CitizenEmail { get; set; } = string.Empty;
        public string? Category { get; set; }
        public ComplaintPriority Priority { get; set; }
        public ComplaintStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int CommentCount { get; set; }
        public bool HasImages { get; set; }
    }
}