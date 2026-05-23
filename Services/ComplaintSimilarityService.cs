using Megdan.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Megdan.Web.Services;

public class ComplaintSimilarityService : IComplaintSimilarityService
{
    private readonly AppDbContext _dbContext;

    public ComplaintSimilarityService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ComplaintSimilarityResult>> FindSimilarComplaintsAsync(
        string? title,
        string? description,
        string? address,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = Normalize(title);
        var normalizedDescription = Normalize(description);
        var normalizedAddress = Normalize(address);

        var inputText = $"{normalizedTitle} {normalizedDescription}".Trim();

        if (string.IsNullOrWhiteSpace(inputText) || inputText.Length < 12)
        {
            return Array.Empty<ComplaintSimilarityResult>();
        }

        var inputTokens = Tokenize(inputText);
        var addressTokens = Tokenize(normalizedAddress);

        var candidates = await _dbContext.Complaints
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(300)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Description,
                c.Category,
                c.Status,
                c.Priority,
                c.Address,
                c.Latitude,
                c.Longitude,
                c.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var results = new List<ComplaintSimilarityResult>();

        foreach (var candidate in candidates)
        {
            var candidateText = Normalize($"{candidate.Title} {candidate.Description}");
            var candidateTokens = Tokenize(candidateText);

            var titleScore = Similarity(normalizedTitle, Normalize(candidate.Title));
            var textScore = Jaccard(inputTokens, candidateTokens);

            double locationScore = 0;
            var candidateAddressTokens = Tokenize(Normalize(candidate.Address));

            if (addressTokens.Count > 0 && candidateAddressTokens.Count > 0)
            {
                locationScore = Jaccard(addressTokens, candidateAddressTokens);
            }

            if (latitude.HasValue && longitude.HasValue &&
    candidate.Latitude.HasValue && candidate.Longitude.HasValue)
            {
                var distanceKm = DistanceKm(
                    latitude.Value,
                    longitude.Value,
                    (double)candidate.Latitude.Value,
                    (double)candidate.Longitude.Value);

                if (distanceKm <= 0.20) locationScore = Math.Max(locationScore, 1.00);
                else if (distanceKm <= 0.50) locationScore = Math.Max(locationScore, 0.80);
                else if (distanceKm <= 1.00) locationScore = Math.Max(locationScore, 0.55);
            }

            var finalScore = (titleScore * 0.35) + (textScore * 0.50) + (locationScore * 0.15);

            if (finalScore < 0.38)
                continue;

            var reason = BuildReason(titleScore, textScore, locationScore);

            results.Add(new ComplaintSimilarityResult
            {
                ComplaintId = candidate.Id,
                Title = candidate.Title,
                Category = candidate.Category ?? "Uncategorized",
                Status = candidate.Status.ToString(),
                Priority = candidate.Priority.ToString(),
                LocationLabel = candidate.Address ?? "Location available",
                CreatedAtUtc = candidate.CreatedAtUtc,
                SimilarityScore = Math.Round(finalScore, 2),
                MatchReason = reason
            });
        }

        return results
            .OrderByDescending(r => r.SimilarityScore)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Take(5)
            .ToList();
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim().ToLowerInvariant();
        value = Regex.Replace(value, @"\s+", " ");
        return value;
    }

    private static HashSet<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new HashSet<string>();

        var tokens = Regex.Split(text, @"[^a-z0-9]+")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => x.Length > 2)
            .ToHashSet();

        return tokens;
    }

    private static double Similarity(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return 0;

        if (left == right)
            return 1;

        var leftTokens = Tokenize(left);
        var rightTokens = Tokenize(right);
        return Jaccard(leftTokens, rightTokens);
    }

    private static double Jaccard(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 || b.Count == 0)
            return 0;

        var intersection = a.Intersect(b).Count();
        var union = a.Union(b).Count();

        return union == 0 ? 0 : (double)intersection / union;
    }

    private static string BuildReason(double titleScore, double textScore, double locationScore)
    {
        var reasons = new List<string>();

        if (titleScore >= 0.60) reasons.Add("similar title");
        if (textScore >= 0.45) reasons.Add("matching description");
        if (locationScore >= 0.55) reasons.Add("same area");

        return reasons.Count == 0 ? "related complaint" : string.Join(", ", reasons);
    }

    private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371;
        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}