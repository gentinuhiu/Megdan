using Megdan.Web.Models;

namespace Megdan.Web.Services
{
    public interface INotificationService
    {
        Task SendComplaintSubmittedAsync(Complaint complaint, string trackingToken);
        Task SendStatusChangedAsync(Complaint complaint);
        Task SendUrgentAlertAsync(Complaint complaint);
        Task SendComplaintAccessLinkAsync(string email, string accessUrl);
    }
}