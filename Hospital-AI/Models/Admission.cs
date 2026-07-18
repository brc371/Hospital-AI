using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_AI.Models
{
    /// <summary>
    /// Represents a patient admission record in the Hospital database.
    /// </summary>
    [Table("Admissions")]
    public class Admission
    {
        /// <summary>The unique identifier of the admission.</summary>
        [Key]
        [Column("admission_id")]
        public int AdmissionId { get; set; }

        /// <summary>The ID of the patient being admitted.</summary>
        [Column("patient_id")]
        public int PatientId { get; set; }

        /// <summary>The ID of the attending physician.</summary>
        [Column("attending_physician_id")]
        public int PhysicianId { get; set; }

        /// <summary>The date and time the patient was admitted.</summary>
        [Column("admission_date")]
        public DateTime AdmissionDate { get; set; }

        /// <summary>The date and time the patient was discharged.</summary>
        [Column("discharge_date")]
        public DateTime DischargeDate { get; set; }

        /// <summary>The name of the department handling the admission.</summary>
        [Column("department")]
        [StringLength(250)]
        public string Department { get; set; } = string.Empty;

        /// <summary>The bed number assigned to the patient.</summary>
        [Column("bed_number")]
        public int BedNumber { get; set; }

        /// <summary>The reason for the patient's visit / admission.</summary>
        [Column("reason_for_visit")]
        public string ReasonForVisit { get; set; } = string.Empty;

        /// <summary>The date and time the record was created.</summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>The date and time the record was last updated.</summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        /// <summary>The patient associated with this admission.</summary>
        [ForeignKey(nameof(PatientId))]
        public Patient? Patient { get; set; }

        /// <summary>The physician associated with this admission.</summary>
        [ForeignKey(nameof(PhysicianId))]
        public Physician? Physician { get; set; }

        /// <summary>Lab results associated with this admission.</summary>
        public ICollection<LabResult> LabResults { get; set; } = [];
    }
}
