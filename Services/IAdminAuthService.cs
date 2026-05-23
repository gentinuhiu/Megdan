using Megdan.Web.Models;

namespace Megdan.Web.Services
{
    public interface IAdminAuthService
    {
        Task<AdminUser?> ValidateCredentialsAsync(string email, string password);
        string HashPassword(AdminUser user, string password);
    }
}