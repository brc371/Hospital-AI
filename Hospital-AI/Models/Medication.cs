using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_AI.Models
{
    /// <summary>
    /// Represents a medication record in the Hospital database.
    /// </summary>
    [Table("Medications")]
    public class Medication
    {
        /// <summary>The unique identifier of the medication record.</summary>
        [Key]
        [Column("medication_id")]
        public int MedicationId { get; set; }

        /// <summary>The ID of the patient this medication is prescribed to.</summary>
        [Column("patient_id")]
        public int PatientId { get; set; }

        /// <summary>The ID of the physician who prescribed this medication.</summary>
        [Column("prescribing_physician_id")]
        public int PrescribingPhysicianId { get; set; }

        /// <summary>The name of the drug.</summary>
        [Column("drug_name")]
        [StringLength(250)]
        public string DrugName { get; set; } = string.Empty;

        /// <summary>The dosage of the medication.</summary>
        [Column("dose")]
        [StringLength(250)]
        public string Dose { get; set; } = string.Empty;

        /// <summary>The frequency at which the medication is taken.</summary>
        [Column("frequency")]
        [StringLength(250)]
        public string Frequency { get; set; } = string.Empty;

        /// <summary>The date the medication was started.</summary>
        [Column("start_date")]
        public DateOnly StartDate { get; set; }

        /// <summary>The date the medication was ended (null if ongoing).</summary>
        [Column("end_date")]
        public DateOnly? EndDate { get; set; }

        /// <summary>The date and time the record was created.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>The date and time the record was last updated.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        /// <summary>The patient this medication is prescribed to.</summary>
        [ForeignKey(nameof(PatientId))]
        public Patient? Patient { get; set; }

        /// <summary>The physician who prescribed this medication.</summary>
        [ForeignKey(nameof(PrescribingPhysicianId))]
        public Physician? Physician { get; set; }
    }
}
