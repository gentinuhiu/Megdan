using System.ComponentModel.DataAnnotations;

namespace Megdan.Web.ViewModels
{
    public class RequestComplaintAccessViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;
    }
}