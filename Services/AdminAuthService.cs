using Megdan.Web.Data;
using Megdan.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Megdan.Web.Services
{
    public class AdminAuthService : IAdminAuthService
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<AdminUser> _passwordHasher;

        public AdminAuthService(AppDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<AdminUser>();
        }

        public async Task<AdminUser?> ValidateCredentialsAsync(string email, string password)
        {
            var user = await _context.AdminUsers
                .FirstOrDefaultAsync(x => x.Email == email && x.IsActive);

            if (user == null)
                return null;

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            return result == PasswordVerificationResult.Failed ? null : user;
        }

        public string HashPassword(AdminUser user, string password)
        {
            return _passwordHasher.HashPassword(user, password);
        }
    }
}