using Hospital_AI.Data;
using Hospital_AI.Models;
using Microsoft.EntityFrameworkCore;

namespace Hospital_AI.Services
{
    /// <summary>
    /// Matches patients by first name, last name, and date of birth. Name comparison is
    /// case-insensitive since providers may type names with different casing across visits.
    /// </summary>
    public class PatientMatchingService : IPatientMatchingService
    {
        private readonly ClinicalScribeDbContext _dbContext;

        /// <summary>Initializes a new instance of <see cref="PatientMatchingService"/>.</summary>
        /// <param name="dbContext">The database context used to look up and create patients.</param>
        public PatientMatchingService(ClinicalScribeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc />
        public async Task<Patient> FindOrCreateAsync(string firstName, string lastName, DateOnly dateOfBirth)
        {
            var normalizedFirstName = firstName.Trim();
            var normalizedLastName = lastName.Trim();

            var existingPatient = await _dbContext.Patients.FirstOrDefaultAsync(p =>
                p.FirstName.ToLower() == normalizedFirstName.ToLower() &&
                p.LastName.ToLower() == normalizedLastName.ToLower() &&
                p.DateOfBirth == dateOfBirth);

            if (existingPatient is not null)
            {
                return existingPatient;
            }

            var newPatient = new Patient
            {
                Id = Guid.NewGuid(),
                FirstName = normalizedFirstName,
                LastName = normalizedLastName,
                DateOfBirth = dateOfBirth
            };

            _dbContext.Patients.Add(newPatient);
            await _dbContext.SaveChangesAsync();

            return newPatient;
        }
    }
}
