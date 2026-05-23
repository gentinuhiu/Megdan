namespace Megdan.Web.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;

        public LocalFileStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<List<(string FileName, string FilePath, string ContentType, long FileSizeBytes)>> SaveComplaintImagesAsync(IEnumerable<IFormFile> files)
        {
            var results = new List<(string FileName, string FilePath, string ContentType, long FileSizeBytes)>();

            var uploadRoot = Path.Combine(_environment.WebRootPath, "uploads", "complaints");
            Directory.CreateDirectory(uploadRoot);

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                    continue;

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png" };

                if (!allowed.Contains(extension))
                    continue;

                var safeName = $"{Guid.NewGuid()}{extension}";
                var physicalPath = Path.Combine(uploadRoot, safeName);

                using var stream = new FileStream(physicalPath, FileMode.Create);
                await file.CopyToAsync(stream);

                var relativePath = $"/uploads/complaints/{safeName}";

                results.Add((safeName, relativePath, file.ContentType ?? "application/octet-stream", file.Length));
            }

            return results;
        }
    }
}