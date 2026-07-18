using Hospital_AI.Models;

namespace Hospital_AI.Services
{
    /// <summary>
    /// Matches a patient by first name, last name, and date of birth, creating a new
    /// <see cref="Patient"/> record if no match exists. Matching this way lets the app detect
    /// returning patients across encounters without requiring a formal MRN/patient ID lookup.
    /// </summary>
    public interface IPatientMatchingService
    {
        /// <summary>
        /// Finds an existing patient matching the given first name, last name, and date of
        /// birth (case-insensitive name match), or creates a new patient record if none exists.
        /// </summary>
        /// <param name="firstName">The patient's first name.</param>
        /// <param name="lastName">The patient's last name.</param>
        /// <param name="dateOfBirth">The patient's date of birth.</param>
        Task<Patient> FindOrCreateAsync(string firstName, string lastName, DateOnly dateOfBirth);
    }
}
