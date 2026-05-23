using System.ComponentModel.DataAnnotations;

namespace Megdan.Web.ViewModels
{
    public class AddComplaintFeedbackViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(2000)]
        public string? Comment { get; set; }
    }
}