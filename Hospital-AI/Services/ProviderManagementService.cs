using Hospital_AI.Data;
using Hospital_AI.Models;
using Microsoft.EntityFrameworkCore;

namespace Hospital_AI.Services
{
    /// <inheritdoc cref="IProviderManagementService" />
    public class ProviderManagementService : IProviderManagementService
    {
        private readonly ClinicalScribeDbContext _dbContext;

        /// <summary>Initializes a new instance of <see cref="ProviderManagementService"/>.</summary>
        public ProviderManagementService(ClinicalScribeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc />
        public async Task<List<Provider>> GetAllAsync()
        {
            return await _dbContext.Providers
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Provider> AddAsync(string name, string email, ProviderRole role)
        {
            var provider = new Provider
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email,
                Role = role,
                IsActive = true
            };

            _dbContext.Providers.Add(provider);
            await _dbContext.SaveChangesAsync();

            return provider;
        }

        /// <inheritdoc />
        public async Task<Provider?> SetActiveAsync(Guid providerId, bool isActive)
        {
            var provider = await _dbContext.Providers.FirstOrDefaultAsync(p => p.Id == providerId);
            if (provider is null)
            {
                return null;
            }

            provider.IsActive = isActive;
            await _dbContext.SaveChangesAsync();

            return provider;
        }
    }
}
