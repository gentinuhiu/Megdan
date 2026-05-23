using System.ComponentModel.DataAnnotations;

namespace Megdan.Web.Models
{
    public class ComplaintComment
    {
        public int Id { get; set; }

        public int ComplaintId { get; set; }
        public Complaint Complaint { get; set; } = null!;

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string AuthorType { get; set; } = string.Empty; // Admin or Citizen

        [MaxLength(100)]
        public string? AuthorName { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}