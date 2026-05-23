using Megdan.Web.Models;
using Megdan.Web.Models.Enums;

namespace Megdan.Web.ViewModels
{
    public class TrackComplaintViewModel
    {
        public string Token { get; set; } = string.Empty;
        public Complaint Complaint { get; set; } = null!;
        public AddCitizenCommentViewModel NewComment { get; set; } = new();
        public AddComplaintFeedbackViewModel NewFeedback { get; set; } = new();

        public bool CanSubmitFeedback =>
            Complaint.Status == ComplaintStatus.Resolved && Complaint.Feedback == null;

        public RequestComplaintAccessViewModel EmailAccessRequest { get; set; } = new();
    }
}