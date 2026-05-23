namespace Megdan.Web.Services.Configuration
{
    public class AiProviderSettings
    {
        public string Provider { get; set; } = "OpenRouter";
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public List<string> FallbackModels { get; set; } = new();
        public int MaxRetries { get; set; } = 2;
    }
}