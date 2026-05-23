using Megdan.Web.Services.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Megdan.Web.Services
{
    public class RecaptchaService : IRecaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly RecaptchaSettings _settings;

        public RecaptchaService(HttpClient httpClient, IOptions<RecaptchaSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> VerifyAsync(string token, string expectedAction)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (false, "reCAPTCHA token is missing.");

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = _settings.SecretKey,
                ["response"] = token
            });

            using var response = await _httpClient.PostAsync(_settings.VerifyUrl, content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, "reCAPTCHA verification failed.");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var success = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
            var score = root.TryGetProperty("score", out var scoreProp) ? scoreProp.GetDouble() : 0.0;
            var action = root.TryGetProperty("action", out var actionProp) ? actionProp.GetString() : null;

            if (!success)
                return (false, "reCAPTCHA validation was not successful.");

            if (!string.Equals(action, expectedAction, StringComparison.OrdinalIgnoreCase))
                return (false, "reCAPTCHA action mismatch.");

            if (score < _settings.MinimumScore)
                return (false, $"reCAPTCHA score too low ({score:0.00}).");

            return (true, string.Empty);
        }
    }
}