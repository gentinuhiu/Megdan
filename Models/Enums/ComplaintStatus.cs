using System.ComponentModel.DataAnnotations;

namespace Megdan.Web.Models.Enums
{
    public enum ComplaintStatus
    {
        [Display(Name = "Pending")]
        Pending = 1,

        [Display(Name = "In Progress")]
        InProgress = 2,

        [Display(Name = "Resolved")]
        Resolved = 3
    }
}