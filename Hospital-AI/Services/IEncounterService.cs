using Hospital_AI.Models;

namespace Hospital_AI.Services
{
    /// <summary>
    /// Manages the lifecycle of a provider's clinical encounters: starting a new encounter for
    /// a patient (matched by first/last/DOB), and persisting the in-progress draft note text so
    /// work can be restored across devices/sessions.
    /// </summary>
    public interface IEncounterService
    {
        /// <summary>
        /// Starts a new encounter for the given provider and patient (matched or created by
        /// first name, last name, and date of birth).
        /// </summary>
        /// <param name="providerId">The provider starting the encounter.</param>
        /// <param name="firstName">The patient's first name.</param>
        /// <param name="lastName">The patient's last name.</param>
        /// <param name="dateOfBirth">The patient's date of birth.</param>
        /// <param name="noteTemplateId">The note template to use for this encounter's generations, if any.</param>
        Task<Encounter> StartEncounterAsync(Guid providerId, string firstName, string lastName, DateOnly dateOfBirth, Guid? noteTemplateId = null);

        /// <summary>
        /// Saves the in-progress transcript and/or draft note text for an encounter, without
        /// creating a new finalized <see cref="NoteVersion"/>. Only the owning provider may
        /// update the draft.
        /// </summary>
        /// <param name="encounterId">The encounter to update.</param>
        /// <param name="providerId">The provider making the update (must own the encounter).</param>
        /// <param name="transcriptText">The updated raw transcript/notes text.</param>
        /// <param name="draftNoteText">The updated draft SOAP note text, if any.</param>
        Task<Encounter?> SaveDraftAsync(Guid encounterId, Guid providerId, string transcriptText, string? draftNoteText);

        /// <summary>
        /// Retrieves an encounter by ID, including its patient. Returns <c>null</c> if not
        /// found, or if the encounter belongs to a different provider than <paramref name="providerId"/>
        /// (unless <paramref name="isAdmin"/> is <c>true</c>, in which case any encounter can be viewed).
        /// </summary>
        /// <param name="encounterId">The encounter to retrieve.</param>
        /// <param name="providerId">The requesting provider's ID.</param>
        /// <param name="isAdmin">Whether the requesting user is an Admin (can view any provider's encounters).</param>
        Task<Encounter?> GetEncounterAsync(Guid encounterId, Guid providerId, bool isAdmin);

        /// <summary>
        /// Retrieves all encounters owned by the given provider, most recently updated first.
        /// </summary>
        /// <param name="providerId">The provider whose encounters to list.</param>
        Task<List<Encounter>> GetEncountersForProviderAsync(Guid providerId);

        /// <summary>
        /// Retrieves every encounter across all providers, including patient and owning
        /// provider details, most recently updated first. Intended for Admin use only (e.g.
        /// the Admin dashboard and cross-provider encounter viewing).
        /// </summary>
        Task<List<Encounter>> GetAllEncountersAsync();

        /// <summary>
        /// Finalizes the encounter's current draft note text into a new, immutable
        /// <see cref="NoteVersion"/> (parsed into Subjective/Objective/Assessment/Plan
        /// sections), records an <see cref="AuditLog"/> entry, and marks the encounter as
        /// <see cref="EncounterStatus.Saved"/>. Only the owning provider may save. Returns
        /// <c>null</c> if the encounter doesn't exist, isn't owned by the given provider, or has
        /// no draft note text to save.
        /// </summary>
        /// <param name="encounterId">The encounter to finalize a note version for.</param>
        /// <param name="providerId">The provider saving the note (must own the encounter).</param>
        Task<NoteVersion?> SaveNoteVersionAsync(Guid encounterId, Guid providerId);

        /// <summary>
        /// Retrieves the saved note version history for an encounter, most recent first.
        /// </summary>
        /// <param name="encounterId">The encounter whose note versions to list.</param>
        Task<List<NoteVersion>> GetNoteVersionsAsync(Guid encounterId);

        /// <summary>
        /// Retrieves a single saved note version by ID, including its parent encounter (with
        /// patient) and the provider who saved it, for read-only viewing. Returns <c>null</c>
        /// if not found, or if the version's encounter belongs to a different provider than
        /// <paramref name="providerId"/> (unless <paramref name="isAdmin"/> is <c>true</c>).
        /// </summary>
        /// <param name="versionId">The note version to retrieve.</param>
        /// <param name="providerId">The requesting provider's ID.</param>
        /// <param name="isAdmin">Whether the requesting user is an Admin (can view any provider's note versions).</param>
        Task<NoteVersion?> GetNoteVersionAsync(Guid versionId, Guid providerId, bool isAdmin);
    }
}
