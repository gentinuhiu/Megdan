namespace Megdan.Web.Services.Configuration
{
    public class RecaptchaSettings
    {
        public string SiteKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string VerifyUrl { get; set; } = "https://www.google.com/recaptcha/api/siteverify";
        public double MinimumScore { get; set; } = 0.5;
    }
}