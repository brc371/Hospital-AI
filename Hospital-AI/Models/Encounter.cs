using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_AI.Models
{
    /// <summary>
    /// Represents a single clinical encounter: the raw transcript/notes entered by a provider
    /// for a patient, along with the currently in-progress (unsaved) draft SOAP note text so
    /// the workspace can be restored across devices/sessions. Finalized, saved notes are
    /// recorded as immutable <see cref="NoteVersion"/> rows and are never overwritten here.
    /// </summary>
    [Table("Encounters")]
    public class Encounter
    {
        /// <summary>The unique identifier of the encounter.</summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>The patient this encounter is for.</summary>
        [Required]
        public Guid PatientId { get; set; }

        /// <summary>The provider who owns this encounter. Providers may only see their own encounters.</summary>
        [Required]
        public Guid ProviderId { get; set; }

        /// <summary>The note template active for this encounter when generation last ran.</summary>
        public Guid? NoteTemplateId { get; set; }

        /// <summary>The raw transcript or freeform clinical observations entered by the provider.</summary>
        [Required]
        public string TranscriptText { get; set; } = string.Empty;

        /// <summary>
        /// The most recently generated/edited SOAP note text that has not yet been saved as a
        /// new <see cref="NoteVersion"/>. Persisted continuously so an in-progress draft can be
        /// restored after a refresh, browser close, or login from a different device.
        /// </summary>
        public string? DraftNoteText { get; set; }

        /// <summary>Whether this encounter currently has an unsaved draft or has been saved at least once.</summary>
        public EncounterStatus Status { get; set; } = EncounterStatus.Draft;

        /// <summary>The date and time (UTC) the encounter was started.</summary>
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>The date and time (UTC) the encounter (including its draft) was last modified.</summary>
        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties

        /// <summary>The patient this encounter belongs to.</summary>
        [ForeignKey(nameof(PatientId))]
        public Patient? Patient { get; set; }

        /// <summary>The provider who owns this encounter.</summary>
        [ForeignKey(nameof(ProviderId))]
        public Provider? Provider { get; set; }

        /// <summary>The note template active for this encounter, if any.</summary>
        [ForeignKey(nameof(NoteTemplateId))]
        public NoteTemplate? NoteTemplate { get; set; }

        /// <summary>The append-only history of saved note versions for this encounter.</summary>
        public ICollection<NoteVersion> NoteVersions { get; set; } = [];
    }
}
