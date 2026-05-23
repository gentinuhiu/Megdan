using Megdan.Web.Models.Enums;

namespace Megdan.Web.ViewModels
{
    public class AdminComplaintListViewModel
    {
        public string? Category { get; set; }
        public ComplaintPriority? Priority { get; set; }
        public ComplaintStatus? Status { get; set; }
        public string? CitizenEmail { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int ResolvedCount { get; set; }
        public int UrgentCount { get; set; }

        public List<AdminComplaintListItemViewModel> Complaints { get; set; } = new();

        public string StatusLabelsJson { get; set; } = "[]";
        public string StatusValuesJson { get; set; } = "[]";
        public string PriorityLabelsJson { get; set; } = "[]";
        public string PriorityValuesJson { get; set; } = "[]";

        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
    }
}