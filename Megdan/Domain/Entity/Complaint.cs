using Megdan.Domain.Enum;

namespace Megdan.Domain.Entity
{
    public class Complaint : BaseEntity
    {
        public string Description { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty; // Used for guest tracking (CR-001)
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Category { get; set; }
        public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;
        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;
        public string TrackingToken { get; set; } = Guid.NewGuid().ToString("N"); // Secure tracking link
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relationship: One complaint can have many comments
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
