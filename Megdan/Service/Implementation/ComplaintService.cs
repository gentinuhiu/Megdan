using Megdan.Domain.Entity;
using Megdan.Domain.Enum;
using Megdan.Repository;
using Megdan.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace Megdan.Service.Implementation
{
    public class ComplaintService : IComplaintService
    {
        private readonly IRepository<Complaint> _complaintRepository;
        private readonly IRepository<Comment> _commentRepository;

        public ComplaintService(IRepository<Complaint> complaintRepo, IRepository<Comment> commentRepo)
        {
            _complaintRepository = complaintRepo;
            _commentRepository = commentRepo;
        }

        public IEnumerable<Complaint> GetAllComplaints()
        {
            // Using selector to get the full object
            return _complaintRepository.GetAll(c => c, orderBy: q => q.OrderByDescending(c => c.CreatedAt));
        }

        public Complaint? GetComplaintByTrackingToken(string token)
        {
            return _complaintRepository.Get(
                selector: c => c,
                predicate: c => c.TrackingToken == token,
                include: q => q.Include(c => c.Comments)
            );
        }

        public Complaint CreateComplaint(Complaint complaint)
        {
            // Business Logic: If priority is Urgent, logic for immediate notification would go here
            return _complaintRepository.Insert(complaint);
        }

        public void AddComment(Guid complaintId, string text, bool isAdmin)
        {
            var comment = new Comment
            {
                ComplaintId = complaintId,
                Text = text,
                Author = isAdmin ? "Admin" : "Citizen"
            };
            _commentRepository.Insert(comment);
        }

        public void UpdateStatus(Guid id, ComplaintStatus status)
        {
            var complaint = _complaintRepository.Get(c => c, predicate: c => c.Id == id);
            if (complaint != null)
            {
                complaint.Status = status;
                _complaintRepository.Update(complaint);
            }
        }
    }
}
