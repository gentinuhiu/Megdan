using Megdan.Web.Data;
using Megdan.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Megdan.Web.Services
{
    public class EmailAccessService : IEmailAccessService
    {
        private readonly AppDbContext _context;

        public EmailAccessService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<EmailAccessToken> CreateTokenAsync(string email)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            var entity = new EmailAccessToken
            {
                Email = email.Trim().ToLowerInvariant(),
                Token = token,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
                IsActive = true
            };

            _context.EmailAccessTokens.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<EmailAccessToken?> GetValidTokenAsync(string token)
        {
            var entity = await _context.EmailAccessTokens
                .FirstOrDefaultAsync(x => x.Token == token);

            if (entity == null)
                return null;

            if (!entity.IsActive || entity.ExpiresAtUtc < DateTime.UtcNow)
                return null;

            return entity;
        }
    }
}