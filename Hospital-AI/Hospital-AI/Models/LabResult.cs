using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_AI.Models
{
    /// <summary>
    /// Represents a lab result record in the Hospital database.
    /// </summary>
    [Table("LabResults")]
    public class LabResult
    {
        /// <summary>The unique identifier of the lab result.</summary>
        [Key]
        [Column("lab_result_id")]
        public int LabResultId { get; set; }

        /// <summary>The ID of the patient this result belongs to.</summary>
        [Column("patient_id")]
        public int PatientId { get; set; }

        /// <summary>The ID of the admission this result is associated with (nullable).</summary>
        [Column("admission_id")]
        public int? AdmissionId { get; set; }

        /// <summary>The name of the lab test performed.</summary>
        [Column("test_name")]
        [StringLength(100)]
        public string TestName { get; set; } = string.Empty;

        /// <summary>The numeric result value of the test.</summary>
        [Column("result_value")]
        public double ResultValue { get; set; }

        /// <summary>The unit of measurement for the result (e.g. mg/dL).</summary>
        [Column("unit")]
        [StringLength(250)]
        public string Unit { get; set; } = string.Empty;

        /// <summary>The normal reference range for this test (e.g. "3.5–5.0").</summary>
        [Column("reference_range")]
        [StringLength(250)]
        public string ReferenceRange { get; set; } = string.Empty;

        /// <summary>The date and time the sample was collected.</summary>
        [Column("date_collected")]
        public DateTime DateCollected { get; set; }

        /// <summary>Indicates whether the result is flagged as critical.</summary>
        [Column("critical_flag")]
        public bool CriticalFlag { get; set; }

        /// <summary>The date and time the record was created.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>The date and time the record was last updated.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        /// <summary>The patient this lab result belongs to.</summary>
        [ForeignKey(nameof(PatientId))]
        public Patient? Patient { get; set; }

        /// <summary>The admission this lab result is associated with.</summary>
        [ForeignKey(nameof(AdmissionId))]
        public Admission? Admission { get; set; }
    }
}
