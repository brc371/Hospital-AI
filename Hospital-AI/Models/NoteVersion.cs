using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_AI.Models
{
    /// <summary>
    /// Represents one immutable, saved version of a SOAP note for an encounter. Every save
    /// creates a new row; prior versions are never overwritten or deleted, providing a full
    /// audit trail of who saved each version and when.
    /// </summary>
    [Table("NoteVersions")]
    public class NoteVersion
    {
        /// <summary>The unique identifier of this note version.</summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>The encounter this note version belongs to.</summary>
        [Required]
        public Guid EncounterId { get; set; }

        /// <summary>The 1-based, monotonically increasing version number for this encounter.</summary>
        public int VersionNumber { get; set; }

        /// <summary>The Subjective section of the SOAP note.</summary>
        public string Subjective { get; set; } = string.Empty;

        /// <summary>The Objective section of the SOAP note.</summary>
        public string Objective { get; set; } = string.Empty;

        /// <summary>The Assessment section of the SOAP note, including suggested ICD-10 codes.</summary>
        public string Assessment { get; set; } = string.Empty;

        /// <summary>The Plan section of the SOAP note.</summary>
        public string Plan { get; set; } = string.Empty;

        /// <summary>The provider who saved this version.</summary>
        [Required]
        public Guid SavedByProviderId { get; set; }

        /// <summary>The date and time (UTC) this version was saved.</summary>
        public DateTimeOffset SavedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties

        /// <summary>The encounter this note version belongs to.</summary>
        [ForeignKey(nameof(EncounterId))]
        public Encounter? Encounter { get; set; }

        /// <summary>The provider who saved this version.</summary>
        [ForeignKey(nameof(SavedByProviderId))]
        public Provider? SavedByProvider { get; set; }
    }
}
