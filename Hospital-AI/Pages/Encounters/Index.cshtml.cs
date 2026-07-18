using Hospital_AI.Models;
using Hospital_AI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hospital_AI.Pages.Encounters
{
    /// <summary>
    /// The provider's encounter workspace landing page: lists the signed-in provider's own
    /// encounters and provides a form to start a new encounter for a patient matched by
    /// first name, last name, and date of birth.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IRoleResolutionService _roleResolutionService;
        private readonly IEncounterService _encounterService;
        private readonly ITemplateService _templateService;

        /// <summary>Initializes a new instance of <see cref="IndexModel"/>.</summary>
        public IndexModel(IRoleResolutionService roleResolutionService, IEncounterService encounterService, ITemplateService templateService)
        {
            _roleResolutionService = roleResolutionService;
            _encounterService = encounterService;
            _templateService = templateService;
        }

        /// <summary>The active note templates providers can choose from before starting an encounter.</summary>
        public List<NoteTemplate> AvailableTemplates { get; private set; } = [];

        /// <summary>The note template selected on the "start encounter" form, if any.</summary>
        [BindProperty]
        public Guid? NoteTemplateId { get; set; }

        /// <summary>The encounters to display: all encounters if Admin, otherwise only the signed-in provider's own.</summary>
        public List<Encounter> Encounters { get; private set; } = [];

        /// <summary>Whether the signed-in user is an Admin (sees all providers' encounters, read-only).</summary>
        public bool IsAdmin { get; private set; }

        /// <summary>The patient's first name entered on the "start encounter" form.</summary>
        [BindProperty]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>The patient's last name entered on the "start encounter" form.</summary>
        [BindProperty]
        public string LastName { get; set; } = string.Empty;

        /// <summary>The patient's date of birth entered on the "start encounter" form.</summary>
        [BindProperty]
        public DateOnly? DateOfBirth { get; set; }

        /// <summary>Handles GET requests: loads the encounter list (all encounters for Admin, own encounters for Provider).</summary>
        public async Task<IActionResult> OnGetAsync()
        {
            var provider = await ResolveCurrentProviderAsync();
            if (provider is null)
            {
                return RedirectToPage("/AccessDenied");
            }

            IsAdmin = provider.Role == ProviderRole.Admin;
            Encounters = IsAdmin
                ? await _encounterService.GetAllEncountersAsync()
                : await _encounterService.GetEncountersForProviderAsync(provider.Id);

            if (!IsAdmin)
            {
                AvailableTemplates = await _templateService.GetActiveAsync();
            }

            return Page();
        }

        /// <summary>
        /// Handles the "start encounter" form submission: matches or creates the patient by
        /// first/last/DOB, starts a new encounter, and redirects into the encounter workspace.
        /// </summary>
        public async Task<IActionResult> OnPostStartEncounterAsync()
        {
            var provider = await ResolveCurrentProviderAsync();
            if (provider is null)
            {
                return RedirectToPage("/AccessDenied");
            }

            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) || DateOfBirth is null)
            {
                ModelState.AddModelError(string.Empty, "First name, last name, and date of birth are all required.");
                Encounters = await _encounterService.GetEncountersForProviderAsync(provider.Id);
                AvailableTemplates = await _templateService.GetActiveAsync();
                return Page();
            }

            var encounter = await _encounterService.StartEncounterAsync(provider.Id, FirstName, LastName, DateOfBirth.Value, NoteTemplateId);

            return RedirectToPage("./Workspace", new { id = encounter.Id });
        }

        private async Task<Provider?> ResolveCurrentProviderAsync()
        {
            var email = User.Identity?.Name;
            return email is null ? null : await _roleResolutionService.ResolveByEmailAsync(email);
        }
    }
}
