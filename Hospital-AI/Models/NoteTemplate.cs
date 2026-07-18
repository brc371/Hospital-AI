using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_AI.Models
{
    /// <summary>
    /// Represents an admin-managed note template: a structured prompt that shapes how the AI
    /// generates SOAP notes for a specific encounter type (e.g. orthopedic follow-up, new
    /// patient evaluation, urgent care visit). Providers select a template before generating
    /// a note. Updates take effect immediately for any provider currently generating notes.
    /// </summary>
    [Table("NoteTemplates")]
    public class NoteTemplate
    {
        /// <summary>The unique identifier of the template.</summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>The display name of the template (e.g. "Orthopedic Follow-up").</summary>
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The prompt text/instructions injected into the AI system message to shape note
        /// generation for this encounter type.
        /// </summary>
        [Required]
        public string PromptText { get; set; } = string.Empty;

        /// <summary>Whether this template is available for providers to select.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>The date and time (UTC) this template was created.</summary>
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// The date and time (UTC) this template was last updated. Providers poll/re-check
        /// this value so an in-progress generation picks up admin edits without a page refresh.
        /// </summary>
        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties

        /// <summary>Encounters that used this template.</summary>
        public ICollection<Encounter> Encounters { get; set; } = [];
    }
}
