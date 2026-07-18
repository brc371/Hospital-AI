using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_AI.Models
{
    /// <summary>
    /// Represents a patient's medical history record in the Hospital database.
    /// </summary>
    [Table("MedicalHistory")]
    public class MedicalHistory
    {
        /// <summary>The unique identifier of the medical history record.</summary>
        [Key]
        [Column("history_id")]
        public int HistoryId { get; set; }

        /// <summary>The ID of the patient this record belongs to.</summary>
        [Column("patient_id")]
        public int PatientId { get; set; }

        /// <summary>The patient's chronic conditions.</summary>
        [Column("chronic_conditions")]
        public string? ChronicConditions { get; set; }

        /// <summary>The patient's family medical history.</summary>
        [Column("family_history")]
        public string? FamilyHistory { get; set; }

        /// <summary>Prior surgeries the patient has had.</summary>
        [Column("prior_surgeries")]
        public string? PriorSurgeries { get; set; }

        /// <summary>Known allergies for the patient.</summary>
        [Column("allergies")]
        public string? Allergies { get; set; }

        /// <summary>Additional clinical notes.</summary>
        [Column("notes")]
        public string? Notes { get; set; }

        /// <summary>The date and time the record was created.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>The date and time the record was last updated.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        /// <summary>The patient this record belongs to.</summary>
        [ForeignKey(nameof(PatientId))]
        public Patient? Patient { get; set; }
    }
}
