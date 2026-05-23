using System.ComponentModel.DataAnnotations;

namespace Megdan.Web.ViewModels
{
    public class AddCitizenCommentViewModel
    {
        [Required]
        [StringLength(2000, MinimumLength = 2)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
    }
}