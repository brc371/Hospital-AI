using Hospital_AI.Models;
using Hospital_AI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hospital_AI.Pages.Encounters
{
    /// <summary>
    /// The encounter workspace for a single encounter: lets the owning provider enter/edit the
    /// transcript and draft note text, saving continuously so work can be restored across
    /// devices/sessions. Admins may view (but not edit) any provider's encounter.
    /// </summary>
    public class WorkspaceModel : PageModel
    {
        private readonly IRoleResolutionService _roleResolutionService;
        private readonly IEncounterService _encounterService;

        /// <summary>Initializes a new instance of <see cref="WorkspaceModel"/>.</summary>
        public WorkspaceModel(IRoleResolutionService roleResolutionService, IEncounterService encounterService)
        {
            _roleResolutionService = roleResolutionService;
            _encounterService = encounterService;
        }

        /// <summary>The encounter being viewed/edited.</summary>
        public Encounter? Encounter { get; private set; }

        /// <summary>Whether the signed-in user is an Admin viewing another provider's encounter (read-only).</summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>The encounter's raw transcript/notes text, bound to the textarea.</summary>
        [BindProperty]
        public string TranscriptText { get; set; } = string.Empty;

        /// <summary>The encounter's in-progress draft SOAP note text, bound to the textarea.</summary>
        [BindProperty]
        public string? DraftNoteText { get; set; }

        /// <summary>Whether the draft was just saved successfully (drives a confirmation message).</summary>
        public bool DraftSaved { get; private set; }

        /// <summary>Whether a finalized note version was just saved successfully.</summary>
        public bool NoteVersionSaved { get; private set; }

        /// <summary>Set if saving a finalized note version failed (e.g. no draft text present).</summary>
        public string? SaveNoteError { get; private set; }

        /// <summary>The saved note version history for this encounter, most recent first.</summary>
        public List<NoteVersion> NoteVersions { get; private set; } = [];

        /// <summary>Handles GET requests: loads the encounter for viewing/editing.</summary>
        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var provider = await ResolveCurrentProviderAsync();
            if (provider is null)
            {
                return RedirectToPage("/AccessDenied");
            }

            var isAdmin = provider.Role == ProviderRole.Admin;
            Encounter = await _encounterService.GetEncounterAsync(id, provider.Id, isAdmin);

            if (Encounter is null)
            {
                return NotFound();
            }

            IsReadOnly = isAdmin && Encounter.ProviderId != provider.Id;
            TranscriptText = Encounter.TranscriptText;
            DraftNoteText = Encounter.DraftNoteText;
            NoteVersions = await _encounterService.GetNoteVersionsAsync(id);

            return Page();
        }

        /// <summary>
        /// Handles the "save draft" form submission: persists the current transcript and draft
        /// note text without creating a finalized note version. Only the owning provider may
        /// save; Admins viewing another provider's encounter cannot.
        /// </summary>
        /// <remarks>
        /// Non-happy-path: if the signed-in user's account has been deactivated by an admin
        /// since the page was loaded (e.g. mid-draft), <see cref="ResolveCurrentProviderAsync"/>
        /// returns <c>null</c> just like an unknown user. A normal browser form submission is
        /// redirected to <c>/AccessDenied</c> as usual, but the client-side autosave script
        /// calls this handler via <c>fetch</c> with an <c>X-Requested-With</c> header; for that
        /// case a 401 response is returned instead so the JS can distinguish "not saved because
        /// deactivated" from "saved successfully" and show a clear, non-silent banner rather
        /// than reporting a false "All changes saved."
        /// </remarks>
        public async Task<IActionResult> OnPostSaveDraftAsync(Guid id)
        {
            var provider = await ResolveCurrentProviderAsync();
            if (provider is null)
            {
                if (IsAjaxRequest())
                {
                    return StatusCode(StatusCodes.Status401Unauthorized);
                }

                return RedirectToPage("/AccessDenied");
            }

            var updatedEncounter = await _encounterService.SaveDraftAsync(id, provider.Id, TranscriptText, DraftNoteText);

            if (updatedEncounter is null)
            {
                return NotFound();
            }

            Encounter = updatedEncounter;
            DraftSaved = true;
            NoteVersions = await _encounterService.GetNoteVersionsAsync(id);

            return Page();
        }

        /// <summary>
        /// Handles the "save note" form submission: finalizes the current draft note text into
        /// a new, immutable <see cref="Models.NoteVersion"/> and records an audit log entry.
        /// Only the owning provider may save. Persists the current transcript/draft text first
        /// so nothing typed since the last "Save draft" click is lost.
        /// </summary>
        public async Task<IActionResult> OnPostSaveNoteAsync(Guid id)
        {
            var provider = await ResolveCurrentProviderAsync();
            if (provider is null)
            {
                return RedirectToPage("/AccessDenied");
            }

            var updatedEncounter = await _encounterService.SaveDraftAsync(id, provider.Id, TranscriptText, DraftNoteText);
            if (updatedEncounter is null)
            {
                return NotFound();
            }

            var noteVersion = await _encounterService.SaveNoteVersionAsync(id, provider.Id);

            if (noteVersion is null)
            {
                SaveNoteError = "Cannot save a note version: the draft SOAP note is empty. Generate or write a note first.";
                Encounter = updatedEncounter;
            }
            else
            {
                NoteVersionSaved = true;
                Encounter = await _encounterService.GetEncounterAsync(id, provider.Id, isAdmin: false);
            }

            NoteVersions = await _encounterService.GetNoteVersionsAsync(id);

            return Page();
        }

        private async Task<Provider?> ResolveCurrentProviderAsync()
        {
            var email = User.Identity?.Name;
            return email is null ? null : await _roleResolutionService.ResolveByEmailAsync(email);
        }

        /// <summary>
        /// Determines whether the current request was made via the client-side autosave/
        /// generate-note <c>fetch</c> calls (which set a custom header) rather than a normal
        /// browser form submission, so failure responses can be tailored appropriately for each.
        /// </summary>
        private bool IsAjaxRequest() => Request.Headers.XRequestedWith == "XMLHttpRequest";
    }
}
