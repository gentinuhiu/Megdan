using Megdan.Web.Data;
using Megdan.Web.Services.Configuration;
using Megdan.Web.ViewModels.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Megdan.Web.Services
{
    public class AiAnalysisService : IAiAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly AiProviderSettings _settings;
        private readonly AppDbContext _context;

        public AiAnalysisService(HttpClient httpClient, IOptions<AiProviderSettings> options, AppDbContext context)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _context = context;
        }

        public async Task<AiComplaintAnalysisResult> AnalyzeComplaintAsync(string title, string description)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey) ||
                string.IsNullOrWhiteSpace(_settings.BaseUrl) ||
                string.IsNullOrWhiteSpace(_settings.Model))
            {
                return BuildRuleBasedFallback(title, description, "AI provider is not configured.");
            }

            var prompt = $$"""
You are analyzing a city complaint for a municipal complaint system.

Return ONLY valid JSON with this exact schema:
{
  "category": "one of: Sanitation, Infrastructure, Safety, Noise, Traffic, Environment, Public Lighting, Water, Other",
  "priority": "one of: Low, Medium, High, Urgent",
  "frustrationLevel": "one of: Low, Medium, High",
  "summary": "short summary under 30 words"
}

Title: {{title}}
Description: {{description}}

Rules:
- Use Urgent only for immediate danger, active flooding, electrical hazard, fire risk, road collapse, exposed wires, major traffic danger, or similar critical threats.
- If uncertain, prefer Medium instead of High or Urgent.
- Return JSON only, no markdown.
""";

            var fallbackModels = (_settings.FallbackModels ?? new List<string>())
    .Where(x => !string.IsNullOrWhiteSpace(x))
    .Where(x => !x.Equals(_settings.Model, StringComparison.OrdinalIgnoreCase))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToList();

            var requestBody = new Dictionary<string, object?>
            {
                ["model"] = _settings.Model,
                ["temperature"] = 0.2,
                ["messages"] = new object[]
                {
        new { role = "system", content = "You are a structured municipal complaint classifier. Return JSON only." },
        new { role = "user", content = prompt }
                }
            };

            if (_settings.Provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase) && fallbackModels.Any())
            {
                requestBody["models"] = fallbackModels;
                requestBody["route"] = "fallback";
            }

            var retries = Math.Max(0, _settings.MaxRetries);

            for (var attempt = 0; attempt <= retries; attempt++)
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{_settings.BaseUrl.TrimEnd('/')}/chat/completions");

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

                if (_settings.Provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase))
                {
                    request.Headers.TryAddWithoutValidation("HTTP-Referer", "http://localhost");
                    request.Headers.TryAddWithoutValidation("X-Title", "Megdan");
                }

                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                try
                {
                    using var response = await _httpClient.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();

                    if ((int)response.StatusCode == 429)
                    {
                        if (attempt < retries)
                        {
                            await Task.Delay(1200 * (attempt + 1));
                            continue;
                        }

                        return BuildRuleBasedFallback(title, description, "AI provider is temporarily rate-limited.");
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        return BuildRuleBasedFallback(
                            title,
                            description,
                            $"AI request failed with status {(int)response.StatusCode}: {json}");
                    }

                    using var outerDoc = JsonDocument.Parse(json);
                    var content = outerDoc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        return BuildRuleBasedFallback(title, description, "AI returned empty content.");
                    }

                    var cleaned = content.Trim();

                    if (cleaned.StartsWith("```json"))
                        cleaned = cleaned[7..].Trim();

                    if (cleaned.StartsWith("```"))
                        cleaned = cleaned[3..].Trim();

                    if (cleaned.EndsWith("```"))
                        cleaned = cleaned[..^3].Trim();

                    using var innerDoc = JsonDocument.Parse(cleaned);

                    return new AiComplaintAnalysisResult
                    {
                        Category = innerDoc.RootElement.TryGetProperty("category", out var category) ? category.GetString() ?? "Other" : "Other",
                        Priority = innerDoc.RootElement.TryGetProperty("priority", out var priority) ? priority.GetString() ?? "Medium" : "Medium",
                        FrustrationLevel = innerDoc.RootElement.TryGetProperty("frustrationLevel", out var frustration) ? frustration.GetString() ?? "Low" : "Low",
                        Summary = innerDoc.RootElement.TryGetProperty("summary", out var summary) ? summary.GetString() ?? string.Empty : string.Empty,
                        Success = true
                    };
                }
                catch (Exception)
                {
                    if (attempt < retries)
                    {
                        await Task.Delay(1200 * (attempt + 1));
                        continue;
                    }

                    return BuildRuleBasedFallback(title, description, "AI provider could not be reached.");
                }
            }

            return BuildRuleBasedFallback(title, description, "AI analysis fallback applied.");
        }

        private AiComplaintAnalysisResult BuildRuleBasedFallback(string title, string description, string reason)
        {
            var text = $"{title} {description}".ToLowerInvariant();

            var category = "Other";
            if (text.Contains("garbage") || text.Contains("waste") || text.Contains("trash") || text.Contains("dump"))
                category = "Sanitation";
            else if (text.Contains("road") || text.Contains("pothole") || text.Contains("sidewalk") || text.Contains("bridge"))
                category = "Infrastructure";
            else if (text.Contains("crime") || text.Contains("unsafe") || text.Contains("attack") || text.Contains("violence"))
                category = "Safety";
            else if (text.Contains("noise") || text.Contains("loud") || text.Contains("music"))
                category = "Noise";
            else if (text.Contains("traffic") || text.Contains("parking") || text.Contains("jam"))
                category = "Traffic";
            else if (text.Contains("pollution") || text.Contains("smoke") || text.Contains("tree") || text.Contains("river"))
                category = "Environment";
            else if (text.Contains("light") || text.Contains("lamp") || text.Contains("streetlight"))
                category = "Public Lighting";
            else if (text.Contains("water") || text.Contains("leak") || text.Contains("sewage") || text.Contains("flood"))
                category = "Water";

            var priority = "Medium";
            if (text.Contains("flood") || text.Contains("fire") || text.Contains("exposed wire") || text.Contains("electrical hazard") || text.Contains("collapse"))
                priority = "Urgent";
            else if (text.Contains("danger") || text.Contains("injury") || text.Contains("urgent") || text.Contains("unsafe"))
                priority = "High";
            else if (text.Contains("minor") || text.Contains("small"))
                priority = "Low";

            var frustration = "Low";
            if (text.Contains("terrible") || text.Contains("angry") || text.Contains("furious") || text.Contains("unacceptable"))
                frustration = "High";
            else if (text.Contains("annoying") || text.Contains("frustrating") || text.Contains("problem"))
                frustration = "Medium";

            return new AiComplaintAnalysisResult
            {
                Category = category,
                Priority = priority,
                FrustrationLevel = frustration,
                Summary = description.Length > 120 ? description[..120] + "..." : description,
                Success = false,
                ErrorMessage = reason
            };
        }

        public async Task<List<int>> FindSimilarComplaintIdsAsync(string title, string description)
        {
            var text = $"{title} {description}".ToLowerInvariant();
            var keywords = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Length >= 4)
                .Distinct()
                .Take(10)
                .ToList();

            if (!keywords.Any())
                return new List<int>();

            var query = _context.Complaints.AsQueryable();

            foreach (var keyword in keywords)
            {
                var k = keyword;
                query = query.Where(c => c.Title.Contains(k) || c.Description.Contains(k));
            }

            return await query
                .OrderByDescending(c => c.CreatedAtUtc)
                .Select(c => c.Id)
                .Take(5)
                .ToListAsync();
        }
    }
}