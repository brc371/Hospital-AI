using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_AI.Models
{
    /// <summary>
    /// Records an auditable action taken within the system, such as saving a note version,
    /// creating/editing a template, or activating/deactivating a provider. Stored in the
    /// database (never in memory or a flat file) to satisfy the audit trail requirement.
    /// </summary>
    [Table("AuditLogs")]
    public class AuditLog
    {
        /// <summary>The unique identifier of the audit log entry.</summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>The type of entity affected, e.g. "NoteVersion", "Provider", "NoteTemplate".</summary>
        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>The identifier of the affected entity.</summary>
        [Required]
        public Guid EntityId { get; set; }

        /// <summary>A short description of the action performed, e.g. "Created", "Deactivated".</summary>
        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        /// <summary>The provider who performed the action.</summary>
        [Required]
        public Guid PerformedByProviderId { get; set; }

        /// <summary>The date and time (UTC) the action occurred.</summary>
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>Optional additional detail about the action.</summary>
        [StringLength(1000)]
        public string? Details { get; set; }

        // Navigation properties

        /// <summary>The provider who performed the action.</summary>
        [ForeignKey(nameof(PerformedByProviderId))]
        public Provider? PerformedByProvider { get; set; }
    }
}
