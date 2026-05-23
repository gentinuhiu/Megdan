using System.ComponentModel.DataAnnotations;

namespace Megdan.Web.Models
{
    public class TrackingToken
    {
        public int Id { get; set; }

        public int ComplaintId { get; set; }
        public Complaint Complaint { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Token { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAtUtc { get; set; }
        public DateTime? LastUsedAtUtc { get; set; }
    }
}