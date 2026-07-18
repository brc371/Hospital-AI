using Hospital_AI.Models;
using Hospital_AI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hospital_AI.Pages.Admin
{
    /// <summary>
    /// The Admin dashboard landing page: shows every encounter across all providers,
    /// filterable by provider and date range. Only Admin accounts may access this page.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IRoleResolutionService _roleResolutionService;
        private readonly IEncounterService _encounterService;
        private readonly IProviderManagementService _providerManagementService;

        /// <summary>Initializes a new instance of <see cref="IndexModel"/>.</summary>
        public IndexModel(
            IRoleResolutionService roleResolutionService,
            IEncounterService encounterService,
            IProviderManagementService providerManagementService)
        {
            _roleResolutionService = roleResolutionService;
            _encounterService = encounterService;
            _providerManagementService = providerManagementService;
        }

        /// <summary>The encounters to display after applying the provider/date-range filters.</summary>
        public List<Encounter> Encounters { get; private set; } = [];

        /// <summary>All providers, used to populate the provider filter dropdown.</summary>
        public List<Provider> Providers { get; private set; } = [];

        /// <summary>The provider ID to filter by, or <c>null</c> for all providers.</summary>
        [BindProperty(SupportsGet = true)]
        public Guid? ProviderId { get; set; }

        /// <summary>The inclusive start date to filter by (encounter's CreatedAtUtc), or <c>null</c> for no lower bound.</summary>
        [BindProperty(SupportsGet = true)]
        public DateOnly? FromDate { get; set; }

        /// <summary>The inclusive end date to filter by (encounter's CreatedAtUtc), or <c>null</c> for no upper bound.</summary>
        [BindProperty(SupportsGet = true)]
        public DateOnly? ToDate { get; set; }

        /// <summary>Handles GET requests: loads all encounters, providers, and applies any active filters.</summary>
        public async Task<IActionResult> OnGetAsync()
        {
            var provider = await ResolveCurrentAdminAsync();
            if (provider is null)
            {
                return RedirectToPage("/AccessDenied");
            }

            Providers = await _providerManagementService.GetAllAsync();

            var allEncounters = await _encounterService.GetAllEncountersAsync();

            IEnumerable<Encounter> filtered = allEncounters;

            if (ProviderId is not null)
            {
                filtered = filtered.Where(e => e.ProviderId == ProviderId);
            }

            if (FromDate is not null)
            {
                var fromUtc = FromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                filtered = filtered.Where(e => e.CreatedAtUtc.UtcDateTime >= fromUtc);
            }

            if (ToDate is not null)
            {
                var toUtc = ToDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
                filtered = filtered.Where(e => e.CreatedAtUtc.UtcDateTime <= toUtc);
            }

            Encounters = filtered.ToList();

            return Page();
        }

        private async Task<Provider?> ResolveCurrentAdminAsync()
        {
            var email = User.Identity?.Name;
            var provider = email is null ? null : await _roleResolutionService.ResolveByEmailAsync(email);
            return provider is { Role: ProviderRole.Admin } ? provider : null;
        }
    }
}
