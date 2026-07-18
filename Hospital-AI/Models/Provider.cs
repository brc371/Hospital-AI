using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_AI.Models
{
    /// <summary>
    /// Represents a clinical staff account (Provider or Admin) authenticated via Entra External ID.
    /// Hard-coded demo accounts are seeded at startup; role is resolved by matching the
    /// signed-in user's email against this table.
    /// </summary>
    [Table("Providers")]
    public class Provider
    {
        /// <summary>The unique identifier of the provider account.</summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>The provider's display name.</summary>
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The email address used to sign in via Entra External ID. Used to resolve the
        /// authenticated user to this provider record.
        /// </summary>
        [Required]
        [StringLength(320)]
        public string Email { get; set; } = string.Empty;

        /// <summary>The role assigned to this account (Provider or Admin).</summary>
        public ProviderRole Role { get; set; } = ProviderRole.Provider;

        /// <summary>
        /// Whether the account is currently active. Deactivated providers cannot start new
        /// encounters, but their historical data is preserved.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>The date and time (UTC) the account was created.</summary>
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties

        /// <summary>Encounters authored by this provider.</summary>
        public ICollection<Encounter> Encounters { get; set; } = [];

        /// <summary>Note versions saved by this provider.</summary>
        public ICollection<NoteVersion> SavedNoteVersions { get; set; } = [];
    }
}
