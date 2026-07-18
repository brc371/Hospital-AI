namespace Hospital_AI.Models
{
    /// <summary>
    /// Data transfer object returned by the API for patient resources.
    /// </summary>
    public class PatientResponse
    {
        /// <summary>The unique identifier of the patient.</summary>
        public int PatientId { get; set; }

        /// <summary>The patient's first name.</summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>The patient's last name.</summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>The patient's gender.</summary>
        public string? Gender { get; set; }

        /// <summary>The patient's phone number.</summary>
        public string? PhoneNumber { get; set; }

        /// <summary>The patient's address.</summary>
        public string? Address { get; set; }

        /// <summary>The name of the patient's emergency contact.</summary>
        public string? EmergencyContactName { get; set; }

        /// <summary>The phone number of the patient's emergency contact.</summary>
        public string? EmergencyContactPhone { get; set; }

        /// <summary>The patient's date of birth.</summary>
        public DateOnly? DateOfBirth { get; set; }

        /// <summary>The patient's national ID.</summary>
        public string? NationalId { get; set; }

        /// <summary>The date and time the record was created.</summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>The date and time the record was last updated.</summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
