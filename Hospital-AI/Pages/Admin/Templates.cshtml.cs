using Hospital_AI.Models;
using Hospital_AI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hospital_AI.Pages.Admin
{
    /// <summary>
    /// Admin page for managing <see cref="NoteTemplate"/> records: viewing, creating, editing,
    /// and deleting the structured prompts providers select before generating a note. Only
    /// Admin accounts may access this page.
    /// </summary>
    public class TemplatesModel : PageModel
    {
        private readonly IRoleResolutionService _roleResolutionService;
        private readonly ITemplateService _templateService;

        /// <summary>Initializes a new instance of <see cref="TemplatesModel"/>.</summary>
        public TemplatesModel(IRoleResolutionService roleResolutionService, ITemplateService templateService)
        {
            _roleResolutionService = roleResolutionService;
            _templateService = templateService;
        }

        /// <summary>All templates, most recently updated first.</summary>
        public List<NoteTemplate> Templates { get; private set; } = [];

        /// <summary>
        /// The ID of the template currently being edited (drives showing the edit form inline),
        /// or <c>null</c> when the form is being used to create a new template.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public Guid? EditId { get; set; }

        /// <summary>The template name, bound to the create/edit form.</summary>
        [BindProperty]
        public string Name { get; set; } = string.Empty;

        /// <summary>The template prompt text, bound to the create/edit form.</summary>
        [BindProperty]
        public string PromptText { get; set; } = string.Empty;

        /// <summary>Whether the template is active, bound to the edit form.</summary>
        [BindProperty]
        public bool IsActive { get; set; } = true;

        /// <summary>Set if saving the template failed validation.</summary>
        public string? SaveError { get; private set; }

        /// <summary>Handles GET requests: loads all templates, and the one being edited (if any) into the form fields.</summary>
        public async Task<IActionResult> OnGetAsync()
        {
            var provider = await ResolveCurrentAdminAsync();
            if (provider is null)
            {
                return RedirectToPage("/AccessDenied");
            }

            Templates = await _templateService.GetAllAsync();

            if (EditId is not null)
            {
                var editing = Templates.FirstOrDefault(t => t.Id == EditId);
                if (editing is not null)
                {
                    Name = editing.Name;
                    PromptText = editing.PromptText;
                    IsActive = editing.IsActive;
                }
            }

            return Page();
        }

        /// <summary>Handles the "create template" form submission.</summary>
        public async Task<IActionResult> OnPostCreateAsync()
        {
            var provider = await ResolveCurrentAdminAsync();
            if (provider is null)
            {
                return RedirectToPage("/AccessDenied");
            }

            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(PromptText))
            {
                SaveError = "Name and prompt text are both required.";
                Templates = await _templateService.GetAllAsync();
                return Page();
            }

            await _templateService.CreateAsync(Name, PromptText);

            return RedirectToPage();
        }

        /// <summary>Handles the "save changes" form submission for an existing template.</summary>
        public async Task<IActionResult> OnPostUpdateAsync(Guid id)
        {
            var provider = await ResolveCurrentAdminAsync();
            if (provider is null)
            {
                return RedirectToPage("/AccessDenied");
            }

            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(PromptText))
            {
                SaveError = "Name and prompt text are both required.";
                Templates = await _templateService.GetAllAsync();
                EditId = id;
                return Page();
            }

            await _templateService.UpdateAsync(id, Name, PromptText, IsActive);

            return RedirectToPage();
        }

        /// <summary>Handles the "delete template" action.</summary>
        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var provider = await ResolveCurrentAdminAsync();
            if (provider is null)
            {
                return RedirectToPage("/AccessDenied");
            }

            await _templateService.DeleteAsync(id);

            return RedirectToPage();
        }

        private async Task<Provider?> ResolveCurrentAdminAsync()
        {
            var email = User.Identity?.Name;
            var provider = email is null ? null : await _roleResolutionService.ResolveByEmailAsync(email);
            return provider is { Role: ProviderRole.Admin } ? provider : null;
        }
    }
}
