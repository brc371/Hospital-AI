using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_AI.Models
{
    /// <summary>
    /// Represents a patient identified by first name, last name, and date of birth.
    /// Patients are matched across encounters using this triplet so that prior
    /// history can be retrieved and injected as AI context for returning patients.
    /// </summary>
    [Table("Patients")]
    public class Patient
    {
        /// <summary>The unique identifier of the patient.</summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>The patient's first name.</summary>
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>The patient's last name.</summary>
        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>The patient's date of birth.</summary>
        [Column(TypeName = "date")]
        public DateOnly DateOfBirth { get; set; }

        /// <summary>The date and time (UTC) this patient record was first created.</summary>
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties

        /// <summary>All encounters recorded for this patient, across all providers.</summary>
        public ICollection<Encounter> Encounters { get; set; } = [];
    }
}
