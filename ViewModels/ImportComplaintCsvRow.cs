namespace Megdan.Web.ViewModels
{
    public class ImportComplaintCsvRow
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CitizenEmail { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public string? Category { get; set; }
        public string? Priority { get; set; }
        public string? Status { get; set; }
    }
}