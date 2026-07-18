using Hospital_AI.Models;
using Hospital_AI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hospital_AI.Pages.Admin
{
    /// <summary>
    /// Admin page for managing the provider/admin roster: viewing all accounts, adding new
    /// ones, and activating/deactivating existing ones. Only Admin accounts may access this page.
    /// </summary>
    public class ProvidersModel : PageModel
    {
        private readonly IRoleResolutionService _roleResolutionService;
        private readonly IProviderManagementService _providerManagementService;

        /// <summary>Initializes a new instance of <see cref="ProvidersModel"/>.</summary>
        public ProvidersModel(IRoleResolutionService roleResolutionService, IProviderManagementService providerManagementService)
        {
            _roleResolutionService = roleResolutionService;
            _providerManagementService = providerManagementService;
        }

        /// <summary>All provider/admin accounts, ordered by name.</summary>
        public List<Provider> Providers { get; private set; } = [];

        /// <summary>The new account's display name, bound to the "add provider" form.</summary>
        [BindProperty]
        public string NewName { get; set; } = string.Empty;

        /// <summary>The new account's sign-in email, bound to the "add provider" form.</summary>
        [BindProperty]
        public string NewEmail { get; set; } = string.Empty;

        /// <summary>The new account's role, bound to the "add provider" form.</summary>
        [BindProperty]
        public ProviderRole NewRole { get; set; } = ProviderRole.Provider;

        /// <summary>Set if adding a new account failed (e.g. duplicate email).</summary>
        public string? AddError { get; private set; }

        /// <summary>Handles GET requests: loads the current provider roster.</summary>
        public async Task<IActionResult> OnGetAsync()
        {
            var provider = await ResolveCurrentAdminAsync();
            if (provider is null)
            {
                return RedirectToPage("/AccessDenied");
            }

            Providers = await _providerManagementService.GetAllAsync();
            return Page();
        }

        /// <summary>Handles the "add provider" form submission.</summary>
        public async Task<IActionResult> OnPostAddAsync()
        {
            var provider = await ResolveCurrentAdminAsync();
            if (provider is null)
            {
                return RedirectToPage("/AccessDenied");
            }

            Providers = await _providerManagementService.GetAllAsync();

            if (string.IsNullOrWhiteSpace(NewName) || string.IsNullOrWhiteSpace(NewEmail))
            {
                AddError = "Name and email are both required.";
                return Page();
            }

            if (Providers.Any(p => string.Equals(p.Email, NewEmail, StringComparison.OrdinalIgnoreCase)))
            {
                AddError = "A provider with that email already exists.";
                return Page();
            }

            await _providerManagementService.AddAsync(NewName, NewEmail, NewRole);
            Providers = await _providerManagementService.GetAllAsync();

            return Page();
        }

        /// <summary>Handles the "toggle active" action for a given provider row.</summary>
        public async Task<IActionResult> OnPostToggleActiveAsync(Guid id, bool isActive)
        {
            var provider = await ResolveCurrentAdminAsync();
            if (provider is null)
            {
                return RedirectToPage("/AccessDenied");
            }

            await _providerManagementService.SetActiveAsync(id, isActive);
            Providers = await _providerManagementService.GetAllAsync();

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
