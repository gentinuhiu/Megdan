using CsvHelper.Configuration;
using Megdan.Web.ViewModels;

namespace Megdan.Web.Helpers
{
    public sealed class ImportComplaintCsvRowMap : ClassMap<ImportComplaintCsvRow>
    {
        public ImportComplaintCsvRowMap()
        {
            Map(m => m.Title).Name("Title");
            Map(m => m.Description).Name("Description");
            Map(m => m.CitizenEmail).Name("CitizenEmail");
            Map(m => m.Address).Name("Address");
            Map(m => m.Latitude).Name("Latitude");
            Map(m => m.Longitude).Name("Longitude");
            Map(m => m.Category).Name("Category");
            Map(m => m.Priority).Name("Priority");
            Map(m => m.Status).Name("Status");
        }
    }
}