namespace Hospital_AI.Models
{
    /// <summary>
    /// Top-level response for the GetPatientSummary endpoint.
    /// Mirrors the three result sets returned by the GetPatientSummary stored procedure:
    /// demographics + medical history, medications, and admissions.
    /// </summary>
    public class PatientSummaryResponse
    {
        /// <summary>Patient demographics and medical history (result set 1).</summary>
        public PatientSummaryDemographics Demographics { get; set; } = new();

        /// <summary>Medication records for the patient (result set 2).</summary>
        public PatientSummaryMedications Medications { get; set; } = new();

        /// <summary>Admission records for the patient (result set 3).</summary>
        public PatientSummaryAdmissions Admissions { get; set; } = new();
    }

    /// <summary>
    /// Patient demographics combined with medical history (result set 1).
    /// </summary>
    public class PatientSummaryDemographics
    {
        /// <summary>The unique identifier of the patient.</summary>
        public int PatientId { get; set; }

        /// <summary>The patient's first name.</summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>The patient's last name.</summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>The patient's date of birth.</summary>
        public DateOnly? DateOfBirth { get; set; }

        /// <summary>The patient's gender.</summary>
        public string? Gender { get; set; }

        /// <summary>The patient's phone number.</summary>
        public string? PhoneNumber { get; set; }

        /// <summary>The name of the patient's emergency contact.</summary>
        public string? EmergencyContactName { get; set; }

        /// <summary>The phone number of the patient's emergency contact.</summary>
        public string? EmergencyContactPhone { get; set; }

        /// <summary>Known chronic conditions (from medical history).</summary>
        public string? ChronicConditions { get; set; }

        /// <summary>Prior surgeries (from medical history).</summary>
        public string? PriorSurgeries { get; set; }

        /// <summary>Known allergies (from medical history).</summary>
        public string? Allergies { get; set; }

        /// <summary>Family medical history.</summary>
        public string? FamilyHistory { get; set; }

        /// <summary>Additional clinical notes (from medical history).</summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Wrapper for the medications result set. Contains a friendly message when
    /// no medication records exist, otherwise contains the list of medications.
    /// </summary>
    public class PatientSummaryMedications
    {
        /// <summary>Friendly message when no medication records are found; null otherwise.</summary>
        public string? Message { get; set; }

        /// <summary>List of medication records for the patient.</summary>
        public List<PatientSummaryMedicationItem> Records { get; set; } = [];
    }

    /// <summary>
    /// A single medication record within the patient summary.
    /// </summary>
    public class PatientSummaryMedicationItem
    {
        /// <summary>The unique identifier of the medication record.</summary>
        public int MedicationId { get; set; }

        /// <summary>The name of the prescribed drug.</summary>
        public string DrugName { get; set; } = string.Empty;

        /// <summary>The dosage of the medication.</summary>
        public string Dose { get; set; } = string.Empty;

        /// <summary>The frequency at which the medication is taken.</summary>
        public string Frequency { get; set; } = string.Empty;

        /// <summary>The date the medication was started.</summary>
        public DateOnly StartDate { get; set; }

        /// <summary>The date the medication ended (null if ongoing).</summary>
        public DateOnly? EndDate { get; set; }

        /// <summary>The first name of the prescribing physician.</summary>
        public string? PrescribingPhysicianFirstName { get; set; }

        /// <summary>The last name of the prescribing physician.</summary>
        public string? PrescribingPhysicianLastName { get; set; }
    }

    /// <summary>
    /// Wrapper for the admissions result set. Contains a friendly message when
    /// no admission records exist, otherwise contains the list of admissions.
    /// </summary>
    public class PatientSummaryAdmissions
    {
        /// <summary>Friendly message when no admission records are found; null otherwise.</summary>
        public string? Message { get; set; }

        /// <summary>List of admission records for the patient.</summary>
        public List<PatientSummaryAdmissionItem> Records { get; set; } = [];
    }

    /// <summary>
    /// A single admission record within the patient summary.
    /// </summary>
    public class PatientSummaryAdmissionItem
    {
        /// <summary>The unique identifier of the admission.</summary>
        public int AdmissionId { get; set; }

        /// <summary>The date and time the patient was admitted.</summary>
        public DateTime AdmissionDate { get; set; }

        /// <summary>The date and time the patient was discharged.</summary>
        public DateTime DischargeDate { get; set; }

        /// <summary>The department that handled the admission.</summary>
        public string Department { get; set; } = string.Empty;

        /// <summary>The bed number assigned to the patient.</summary>
        public int BedNumber { get; set; }

        /// <summary>The reason for the patient's visit.</summary>
        public string ReasonForVisit { get; set; } = string.Empty;

        /// <summary>The first name of the attending physician.</summary>
        public string? AttendingPhysicianFirstName { get; set; }

        /// <summary>The last name of the attending physician.</summary>
        public string? AttendingPhysicianLastName { get; set; }
    }
}
