using System.ComponentModel.DataAnnotations;

namespace Megdan.Web.ViewModels
{
    public class SubmitComplaintViewModel
    {
        [Required]
        [StringLength(150)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(4000, MinimumLength = 10)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        [Display(Name = "Citizen Email")]
        public string CitizenEmail { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Latitude")]
        public decimal? Latitude { get; set; }

        [Display(Name = "Longitude")]
        public decimal? Longitude { get; set; }

        [Display(Name = "Images")]
        public List<IFormFile> Images { get; set; } = new();

        public string RecaptchaToken { get; set; } = string.Empty;
        public string RecaptchaSiteKey { get; set; } = string.Empty;
    }
}