using Megdan.Web.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Megdan.Web.Models
{
    public class Complaint
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string CitizenEmail { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        public ComplaintPriority Priority { get; set; } = ComplaintPriority.Medium;
        public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;

        [MaxLength(50)]
        public string? FrustrationLevel { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? Neighborhood { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public bool IsMapApproximate { get; set; }
        public bool IsAiProcessed { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public DateTime? ResolvedAtUtc { get; set; }

        public ICollection<ComplaintComment> Comments { get; set; } = new List<ComplaintComment>();
        public ICollection<ComplaintImage> Images { get; set; } = new List<ComplaintImage>();
        public ICollection<TrackingToken> TrackingTokens { get; set; } = new List<TrackingToken>();
        public ComplaintFeedback? Feedback { get; set; }
    }
}