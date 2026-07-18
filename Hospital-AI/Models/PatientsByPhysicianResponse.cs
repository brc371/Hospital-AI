namespace Hospital_AI.Models
{
    /// <summary>
    /// Data transfer object returned by the API for the GetPatientsByPhysician procedure.
    /// Contains physician information, patient demographics, admission details,
    /// department, and reason for visit.
    /// </summary>
    public class PatientsByPhysicianResponse
    {
        // ── Physician ────────────────────────────────────────────────────────
        /// <summary>The unique identifier of the physician.</summary>
        public int PhysicianId { get; set; }

        /// <summary>The physician's first name.</summary>
        public string PhysicianFirstName { get; set; } = string.Empty;

        /// <summary>The physician's last name.</summary>
        public string PhysicianLastName { get; set; } = string.Empty;

        /// <summary>The physician's specialization.</summary>
        public string? Specialization { get; set; }

        // ── Patient Demographics ─────────────────────────────────────────────
        /// <summary>The unique identifier of the patient.</summary>
        public int PatientId { get; set; }

        /// <summary>The patient's first name.</summary>
        public string PatientFirstName { get; set; } = string.Empty;

        /// <summary>The patient's last name.</summary>
        public string PatientLastName { get; set; } = string.Empty;

        /// <summary>The patient's gender.</summary>
        public string? Gender { get; set; }

        /// <summary>The patient's date of birth.</summary>
        public DateOnly? DateOfBirth { get; set; }

        /// <summary>The patient's phone number.</summary>
        public string? PhoneNumber { get; set; }

        // ── Admission Details ────────────────────────────────────────────────
        /// <summary>The unique identifier of the admission.</summary>
        public int AdmissionId { get; set; }

        /// <summary>The date and time the patient was admitted.</summary>
        public DateTime AdmissionDate { get; set; }

        /// <summary>The date and time the patient was discharged.</summary>
        public DateTime DischargeDate { get; set; }

        /// <summary>The reason for the patient's visit.</summary>
        public string? ReasonForVisit { get; set; }

        // ── Department ───────────────────────────────────────────────────────
        /// <summary>The name of the department handling the admission.</summary>
        public string? DepartmentName { get; set; }
    }
}
