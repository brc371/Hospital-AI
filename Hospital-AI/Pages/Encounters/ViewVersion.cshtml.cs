using Hospital_AI.Models;
using Hospital_AI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hospital_AI.Pages.Encounters
{
    /// <summary>
    /// Read-only view of a single saved <see cref="NoteVersion"/>, letting a provider (or
    /// admin) open a prior version from the encounter's version history and see exactly what
    /// was saved at that point in time, including who saved it and when. Note versions are
    /// immutable, so this page never allows editing.
    /// </summary>
    public class ViewVersionModel : PageModel
    {
        private readonly IRoleResolutionService _roleResolutionService;
        private readonly IEncounterService _encounterService;

        /// <summary>Initializes a new instance of <see cref="ViewVersionModel"/>.</summary>
        public ViewVersionModel(IRoleResolutionService roleResolutionService, IEncounterService encounterService)
        {
            _roleResolutionService = roleResolutionService;
            _encounterService = encounterService;
        }

        /// <summary>The note version being viewed.</summary>
        public NoteVersion? NoteVersion { get; private set; }

        /// <summary>Handles GET requests: loads the requested note version for read-only viewing.</summary>
        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var email = User.Identity?.Name;
            var provider = email is null ? null : await _roleResolutionService.ResolveByEmailAsync(email);
            if (provider is null)
            {
                return RedirectToPage("/AccessDenied");
            }

            var isAdmin = provider.Role == ProviderRole.Admin;
            NoteVersion = await _encounterService.GetNoteVersionAsync(id, provider.Id, isAdmin);

            if (NoteVersion is null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
