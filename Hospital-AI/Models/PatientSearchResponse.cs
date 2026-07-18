namespace Hospital_AI.Models
{
    /// <summary>
    /// Data transfer object returned by the SearchPatientsByName endpoint.
    /// Contains the subset of patient fields returned by the SearchPatientsByName stored procedure.
    /// </summary>
    public class PatientSearchResponse
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
    }
}
