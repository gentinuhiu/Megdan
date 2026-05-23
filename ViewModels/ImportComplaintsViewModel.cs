using System.ComponentModel.DataAnnotations;

namespace Megdan.Web.ViewModels
{
    public class ImportComplaintsViewModel
    {
        [Required]
        public IFormFile CsvFile { get; set; } = null!;
    }
}