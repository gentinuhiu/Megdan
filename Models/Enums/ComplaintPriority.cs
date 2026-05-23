using System.ComponentModel.DataAnnotations;

namespace Megdan.Web.Models.Enums
{
    public enum ComplaintPriority
    {
        [Display(Name = "Low")]
        Low = 1,

        [Display(Name = "Medium")]
        Medium = 2,

        [Display(Name = "High")]
        High = 3,

        [Display(Name = "Urgent")]
        Urgent = 4
    }
}