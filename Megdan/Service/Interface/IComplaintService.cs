using Megdan.Domain.Entity;
using Megdan.Domain.Enum;

namespace Megdan.Service.Interface
{
    public interface IComplaintService
    {
        IEnumerable<Complaint> GetAllComplaints();
        Complaint? GetComplaintByTrackingToken(string token);
        Complaint CreateComplaint(Complaint complaint);
        void AddComment(Guid complaintId, string text, bool isAdmin);
        void UpdateStatus(Guid id, ComplaintStatus status);
    }
}
