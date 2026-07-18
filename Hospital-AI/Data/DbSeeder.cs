using Hospital_AI.Models;
using Microsoft.EntityFrameworkCore;

namespace Hospital_AI.Data
{
    /// <summary>
    /// Seeds demo Provider/Admin accounts into the database at application startup. This is a
    /// simple, explainable approach for a demo/interview project: hard-coded accounts rather
    /// than a self-service sign-up flow. Seeding is idempotent - it only runs if the
    /// Providers table is empty.
    /// </summary>
    public static class DbSeeder
    {
        /// <summary>
        /// Seeds 3 demo Provider accounts and 1 Admin account if the Providers table is empty.
        /// One of the demo accounts uses the developer's real Entra External ID email so sign-in
        /// can be tested end-to-end locally and in Azure.
        /// </summary>
        /// <param name="context">The database context to seed.</param>
        public static async Task SeedAsync(ClinicalScribeDbContext context)
        {
            // Idempotent: don't reseed if any providers already exist.
            if (await context.Providers.AnyAsync())
            {
                return;
            }

            var demoAccounts = new List<Provider>
            {
                new Provider
                {
                    Id = Guid.NewGuid(),
                    Name = "Admin",
                    Email = "bcalderon_e94@outlook.com",
                    Role = ProviderRole.Admin,
                    IsActive = true
                },
                new Provider
                {
                    Id = Guid.NewGuid(),
                    Name = "Provider1",
                    Email = "hospitalprovider1.gizmo280@passinbox.com",
                    Role = ProviderRole.Provider,
                    IsActive = true
                },
                new Provider
                {
                    Id = Guid.NewGuid(),
                    Name = "Provider2",
                    Email = "hospitalprovider2.swimming970@passinbox.com",
                    Role = ProviderRole.Provider,
                    IsActive = true
                },
                new Provider
                {
                    Id = Guid.NewGuid(),
                    Name = "Provider3",
                    Email = "brian.r.calderon@proton.me",
                    Role = ProviderRole.Provider,
                    IsActive = true
                }
            };

            context.Providers.AddRange(demoAccounts);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seeds the embedded ICD-10 code reference list into the Icd10Codes table if it is
        /// empty. Idempotent - safe to call on every startup.
        /// </summary>
        /// <param name="context">The database context to seed.</param>
        public static async Task SeedIcd10CodesAsync(ClinicalScribeDbContext context)
        {
            if (await context.Icd10Codes.AnyAsync())
            {
                return;
            }

            var codes = Icd10CodeSeedData.Codes
                .Select(entry => new Icd10Code
                {
                    Id = Guid.NewGuid(),
                    Code = entry.Code,
                    Description = entry.Description
                })
                .ToList();

            context.Icd10Codes.AddRange(codes);
            await context.SaveChangesAsync();
        }
    }
}
