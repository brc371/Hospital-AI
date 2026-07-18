using Hospital_AI.Data;
using Hospital_AI.Models;
using Microsoft.EntityFrameworkCore;

namespace Hospital_AI.Services
{
    /// <summary>
    /// Resolves signed-in Entra External ID users to their <see cref="Provider"/> record by
    /// matching email addresses. Matching is case-insensitive since email address casing is
    /// not significant and Entra may return it with different casing than it was seeded with.
    /// </summary>
    public class RoleResolutionService : IRoleResolutionService
    {
        private readonly ClinicalScribeDbContext _dbContext;

        /// <summary>Initializes a new instance of <see cref="RoleResolutionService"/>.</summary>
        /// <param name="dbContext">The database context used to look up providers.</param>
        public RoleResolutionService(ClinicalScribeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc />
        public async Task<Provider?> ResolveByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();

            var provider = await _dbContext.Providers
                .FirstOrDefaultAsync(p => p.Email.ToLower() == normalizedEmail);

            // Deactivated providers are treated the same as unknown users: they cannot access
            // the app, but their historical data (encounters, note versions) is preserved.
            if (provider is { IsActive: false })
            {
                return null;
            }

            return provider;
        }
    }
}
