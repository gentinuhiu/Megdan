namespace Megdan.Domain.Entity
{
    public class Comment : BaseEntity
    {
        public Guid ComplaintId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Author { get; set; } = "Guest"; // Or "Admin"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Complaint? Complaint { get; set; }
    }
}
