namespace Megdan.Web.Services
{
    public interface IFileStorageService
    {
        Task<List<(string FileName, string FilePath, string ContentType, long FileSizeBytes)>>
            SaveComplaintImagesAsync(IEnumerable<IFormFile> files);
    }
}