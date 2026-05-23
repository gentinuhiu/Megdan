using System.ComponentModel.DataAnnotations;

namespace Megdan.Web.Models
{
    public class ComplaintImage
    {
        public int Id { get; set; }

        public int ComplaintId { get; set; }
        public Complaint Complaint { get; set; } = null!;

        [Required]
        [MaxLength(260)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }
        public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    }
}