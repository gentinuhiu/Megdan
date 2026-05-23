namespace Megdan.Web.Models
{
    public class EmailAccessToken
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime? UsedAtUtc { get; set; }

        public bool IsActive { get; set; } = true;
    }
}