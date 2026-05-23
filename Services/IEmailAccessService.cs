using Megdan.Web.Models;

namespace Megdan.Web.Services
{
    public interface IEmailAccessService
    {
        Task<EmailAccessToken> CreateTokenAsync(string email);
        Task<EmailAccessToken?> GetValidTokenAsync(string token);
    }
}