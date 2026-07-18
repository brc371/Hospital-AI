namespace Hospital_AI.Models
{
    /// <summary>
    /// Data transfer object returned by the API for a single lab result row,
    /// including the owning patient's name.
    /// </summary>
    public class LabResultResponse
    {
        /// <summary>The unique identifier of the lab result.</summary>
        public int LabResultId { get; set; }

        /// <summary>The unique identifier of the patient.</summary>
        public int PatientId { get; set; }

        /// <summary>The patient's first name.</summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>The patient's last name.</summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>The ID of the admission this result is linked to (nullable).</summary>
        public int? AdmissionId { get; set; }

        /// <summary>The name of the lab test performed.</summary>
        public string TestName { get; set; } = string.Empty;

        /// <summary>The numeric result value of the test.</summary>
        public double ResultValue { get; set; }

        /// <summary>The unit of measurement (e.g. mg/dL).</summary>
        public string? Unit { get; set; }

        /// <summary>The normal reference range for this test (e.g. "3.5–5.0").</summary>
        public string? ReferenceRange { get; set; }

        /// <summary>The date and time the sample was collected.</summary>
        public DateTime DateCollected { get; set; }

        /// <summary>Indicates whether the result is flagged as critical.</summary>
        public bool CriticalFlag { get; set; }
    }
}
