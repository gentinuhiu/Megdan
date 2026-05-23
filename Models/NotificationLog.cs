using System.ComponentModel.DataAnnotations;

namespace Megdan.Web.Models
{
    public class NotificationLog
    {
        public int Id { get; set; }

        public int? ComplaintId { get; set; }
        public Complaint? Complaint { get; set; }

        [Required]
        [MaxLength(256)]
        public string RecipientEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string NotificationType { get; set; } = string.Empty;

        public bool IsSuccess { get; set; }

        [MaxLength(2000)]
        public string? ErrorMessage { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}