using Hospital_AI.Models;
using Hospital_AI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hospital_AI.Pages.Encounters
{
    /// <summary>
    /// Read-only side-by-side diff view between two saved <see cref="NoteVersion"/> records for
    /// the same encounter, showing exactly which lines were added/removed/unchanged in each SOAP
    /// section. Pioneer feature: lets a provider quickly see what changed between versions
    /// instead of having to compare two full note versions manually.
    /// </summary>
    public class CompareVersionsModel : PageModel
    {
        private readonly IRoleResolutionService _roleResolutionService;
        private readonly IEncounterService _encounterService;
        private readonly INoteVersionDiffService _diffService;

        /// <summary>Initializes a new instance of <see cref="CompareVersionsModel"/>.</summary>
        public CompareVersionsModel(
            IRoleResolutionService roleResolutionService,
            IEncounterService encounterService,
            INoteVersionDiffService diffService)
        {
            _roleResolutionService = roleResolutionService;
            _encounterService = encounterService;
            _diffService = diffService;
        }

        /// <summary>The older of the two versions being compared.</summary>
        public NoteVersion? OlderVersion { get; private set; }

        /// <summary>The newer of the two versions being compared.</summary>
        public NoteVersion? NewerVersion { get; private set; }

        /// <summary>The computed diff for the Subjective section.</summary>
        public List<DiffLine> SubjectiveDiff { get; private set; } = [];

        /// <summary>The computed diff for the Objective section.</summary>
        public List<DiffLine> ObjectiveDiff { get; private set; } = [];

        /// <summary>The computed diff for the Assessment section.</summary>
        public List<DiffLine> AssessmentDiff { get; private set; } = [];

        /// <summary>The computed diff for the Plan section.</summary>
        public List<DiffLine> PlanDiff { get; private set; } = [];

        /// <summary>
        /// Handles GET requests: loads both note versions (scoped to the requesting provider
        /// unless they're an Admin), orders them chronologically by version number, and
        /// computes a per-section line diff.
        /// </summary>
        /// <param name="fromId">The ID of one of the two note versions to compare.</param>
        /// <param name="toId">The ID of the other note version to compare.</param>
        public async Task<IActionResult> OnGetAsync(Guid fromId, Guid toId)
        {
            var email = User.Identity?.Name;
            var provider = email is null ? null : await _roleResolutionService.ResolveByEmailAsync(email);
            if (provider is null)
            {
                return RedirectToPage("/AccessDenied");
            }

            var isAdmin = provider.Role == ProviderRole.Admin;

            var first = await _encounterService.GetNoteVersionAsync(fromId, provider.Id, isAdmin);
            var second = await _encounterService.GetNoteVersionAsync(toId, provider.Id, isAdmin);

            if (first is null || second is null || first.EncounterId != second.EncounterId)
            {
                return NotFound();
            }

            // Always show the diff in chronological order (older -> newer), regardless of the
            // order the two version IDs were passed in the query string.
            if (first.VersionNumber <= second.VersionNumber)
            {
                OlderVersion = first;
                NewerVersion = second;
            }
            else
            {
                OlderVersion = second;
                NewerVersion = first;
            }

            SubjectiveDiff = _diffService.ComputeDiff(OlderVersion.Subjective, NewerVersion.Subjective);
            ObjectiveDiff = _diffService.ComputeDiff(OlderVersion.Objective, NewerVersion.Objective);
            AssessmentDiff = _diffService.ComputeDiff(OlderVersion.Assessment, NewerVersion.Assessment);
            PlanDiff = _diffService.ComputeDiff(OlderVersion.Plan, NewerVersion.Plan);

            return Page();
        }
    }
}
