using System.ComponentModel.DataAnnotations;

namespace Megdan.Web.Models
{
    public class ComplaintFeedback
    {
        public int Id { get; set; }

        public int ComplaintId { get; set; }
        public Complaint Complaint { get; set; } = null!;

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }

        public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    }
}