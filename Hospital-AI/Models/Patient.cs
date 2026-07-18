using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_AI.Models
{
    /// <summary>
    /// Represents a patient record in the Hospital database.
    /// </summary>
    [Table("Patients")]
    public class Patient
    {
        /// <summary>The unique identifier of the patient.</summary>
        [Key]
        [Column("patient_id")]
        public int PatientId { get; set; }

        /// <summary>The patient's first name.</summary>
        [Column("first_name")]
        [StringLength(250)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>The patient's last name.</summary>
        [Column("last_name")]
        [StringLength(250)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>The patient's gender.</summary>
        [Column("gender")]
        [StringLength(50)]
        public string? Gender { get; set; }

        /// <summary>The patient's phone number.</summary>
        [Column("phone_number")]
        [StringLength(50)]
        public string? PhoneNumber { get; set; }

        /// <summary>The patient's address.</summary>
        [Column("address")]
        [StringLength(255)]
        public string? Address { get; set; }

        /// <summary>The name of the patient's emergency contact.</summary>
        [Column("emergency_contact_name")]
        [StringLength(250)]
        public string? EmergencyContactName { get; set; }

        /// <summary>The phone number of the patient's emergency contact.</summary>
        [Column("emergency_contact_phone")]
        [StringLength(50)]
        public string? EmergencyContactPhone { get; set; }

        /// <summary>The patient's date of birth.</summary>
        [Column("date_of_birth", TypeName = "date")]
        public DateOnly? DateOfBirth { get; set; }

        /// <summary>The patient's national ID.</summary>
        [Column("national_id")]
        [StringLength(50)]
        public string? NationalId { get; set; }

        /// <summary>The date and time the record was created.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>The date and time the record was last updated.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        /// <summary>Admissions for this patient.</summary>
        public ICollection<Admission> Admissions { get; set; } = [];

        /// <summary>Lab results for this patient.</summary>
        public ICollection<LabResult> LabResults { get; set; } = [];

        /// <summary>Medications prescribed to this patient.</summary>
        public ICollection<Medication> Medications { get; set; } = [];

        /// <summary>Medical history for this patient.</summary>
        public MedicalHistory? MedicalHistory { get; set; }
    }
}
