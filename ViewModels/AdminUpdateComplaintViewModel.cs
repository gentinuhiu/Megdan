using Megdan.Web.Models;
using Megdan.Web.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Megdan.Web.ViewModels
{
    public class AdminUpdateComplaintViewModel
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [Required]
        public ComplaintPriority Priority { get; set; }

        [Required]
        public ComplaintStatus Status { get; set; }

        [StringLength(2000)]
        public string? AdminComment { get; set; }

        public Complaint? Complaint { get; set; }
    }
}