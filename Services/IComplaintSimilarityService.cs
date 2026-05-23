namespace Megdan.Web.Services;

public interface IComplaintSimilarityService
{
    Task<IReadOnlyList<ComplaintSimilarityResult>> FindSimilarComplaintsAsync(
        string? title,
        string? description,
        string? address,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken = default);
}