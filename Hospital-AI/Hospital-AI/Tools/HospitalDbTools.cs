using Hospital_AI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace Hospital_AI.Tools
{
    /// <summary>
    /// Provides AI-callable tools for querying the hospital database.
    /// Each method is decorated with <see cref="DescriptionAttribute"/> so the
    /// AI model knows when and how to call it.
    /// </summary>
    public class HospitalDbTools
    {
        private readonly MyDataBaseContext _db;

        /// <summary>
        /// Initializes a new instance of <see cref="HospitalDbTools"/>.
        /// </summary>
        public HospitalDbTools(MyDataBaseContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Returns a list of AI functions (tools) the chat controller can pass to the model.
        /// </summary>
        public IList<AITool> GetTools() => new List<AITool>
        {
            // original tools
            AIFunctionFactory.Create(GetTotalPatientCount,          "get_total_patient_count"),
            AIFunctionFactory.Create(GetPatientsByFirstName,        "get_patients_by_first_name"),
            AIFunctionFactory.Create(GetPatientsByGender,           "get_patients_by_gender"),
            AIFunctionFactory.Create(GetRecentPatients,             "get_recent_patients"),
            // stored-procedure mirrors
            AIFunctionFactory.Create(SearchPatientsByName,          "search_patients_by_name"),
            AIFunctionFactory.Create(GetPatientLabHistory,          "get_patient_lab_history"),
            AIFunctionFactory.Create(GetPatientsWithCriticalLabs,   "get_patients_with_critical_labs"),
            AIFunctionFactory.Create(GetCurrentlyAdmittedPatients,  "get_currently_admitted_patients"),
            AIFunctionFactory.Create(GetPatientsByPhysician,        "get_patients_by_physician"),
            AIFunctionFactory.Create(GetPatientSummary,             "get_patient_summary"),
        };

        // ------------------------------------------------------------------ tools

        [Description("Returns the total number of patients currently in the hospital database.")]
        private async Task<string> GetTotalPatientCount()
        {
            var count = await _db.Patients.AsNoTracking().CountAsync();
            return $"There are {count} patients in the database.";
        }

        [Description("Returns all patients whose first name matches the provided value (case-insensitive). " +
                     "Also returns the total count.")]
        private async Task<string> GetPatientsByFirstName(
            [Description("The first name to search for, e.g. \"Maria\".")] string firstName)
        {
            var matches = await _db.Patients
                .AsNoTracking()
                .Where(p => p.FirstName.ToLower() == firstName.ToLower())
                .Select(p => new { p.PatientId, p.FirstName, p.LastName, p.DateOfBirth, p.Gender })
                .ToListAsync();

            if (matches.Count == 0)
                return $"No patients found with the first name '{firstName}'.";

            var lines = matches.Select(p =>
                $"  - ID {p.PatientId}: {p.FirstName} {p.LastName}, " +
                $"Gender: {p.Gender ?? "N/A"}, " +
                $"DOB: {p.DateOfBirth?.ToString("yyyy-MM-dd") ?? "N/A"}");

            return $"Found {matches.Count} patient(s) named '{firstName}':\n" + string.Join("\n", lines);
        }

        [Description("Returns the count and list of patients filtered by gender. " +
                     "Accepted values: 'Male', 'Female', 'Other'.")]
        private async Task<string> GetPatientsByGender(
            [Description("The gender to filter by: 'Male', 'Female', or 'Other'.")] string gender)
        {
            var matches = await _db.Patients
                .AsNoTracking()
                .Where(p => p.Gender != null && p.Gender.ToLower() == gender.ToLower())
                .Select(p => new { p.PatientId, p.FirstName, p.LastName })
                .ToListAsync();

            if (matches.Count == 0)
                return $"No patients found with gender '{gender}'.";

            var lines = matches.Select(p => $"  - ID {p.PatientId}: {p.FirstName} {p.LastName}");
            return $"Found {matches.Count} patient(s) with gender '{gender}':\n" + string.Join("\n", lines);
        }

        [Description("Returns the most recently added patients, up to the requested limit (max 20).")]
        private async Task<string> GetRecentPatients(
            [Description("How many recent patients to return (1–20).")] int count)
        {
            count = Math.Clamp(count, 1, 20);

            var patients = await _db.Patients
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .Select(p => new { p.PatientId, p.FirstName, p.LastName, p.CreatedAt })
                .ToListAsync();

            if (patients.Count == 0)
                return "No patients found in the database.";

            var lines = patients.Select(p =>
                $"  - ID {p.PatientId}: {p.FirstName} {p.LastName}, " +
                $"Added: {p.CreatedAt?.ToString("yyyy-MM-dd") ?? "N/A"}");

            return $"Most recent {patients.Count} patient(s):\n" + string.Join("\n", lines);
        }

        // ------------------------------------------------------------------ stored-procedure mirrors

        [Description("Searches for patients by first name, last name, or full name (case-insensitive, partial match). " +
                     "The search text must be at least 2 characters. " +
                     "Returns patient ID, full name, date of birth, gender, and phone number.")]
        private async Task<string> SearchPatientsByName(
            [Description("The name (or partial name) to search for, e.g. \"Maurice\" or \"Mo\".")] string searchText)
        {
            searchText = searchText.Trim();

            if (searchText.Length < 2)
                return "Search text must be at least 2 characters long.";

            var matches = await _db.Patients
                .AsNoTracking()
                .Where(p => p.FirstName.Contains(searchText) ||
                            p.LastName.Contains(searchText)  ||
                            (p.FirstName + " " + p.LastName).Contains(searchText))
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Select(p => new { p.PatientId, p.FirstName, p.LastName, p.DateOfBirth, p.Gender, p.PhoneNumber })
                .ToListAsync();

            if (matches.Count == 0)
                return $"No patients found matching '{searchText}'.";

            var lines = matches.Select(p =>
                $"  - ID {p.PatientId}: {p.FirstName} {p.LastName}, " +
                $"DOB: {p.DateOfBirth?.ToString("yyyy-MM-dd") ?? "N/A"}, " +
                $"Gender: {p.Gender ?? "N/A"}, " +
                $"Phone: {p.PhoneNumber ?? "N/A"}");

            return $"Found {matches.Count} patient(s) matching '{searchText}':\n" + string.Join("\n", lines);
        }

        [Description("Returns the complete lab result history for a specific patient, ordered by most recent first. " +
                     "Includes test name, result value, unit, reference range, collection date, and critical flag.")]
        private async Task<string> GetPatientLabHistory(
            [Description("The patient ID to retrieve lab history for.")] int patientId)
        {
            var exists = await _db.Patients.AsNoTracking().AnyAsync(p => p.PatientId == patientId);
            if (!exists)
                return $"Patient with ID {patientId} does not exist.";

            var labs = await _db.LabResults
                .AsNoTracking()
                .Where(lr => lr.PatientId == patientId)
                .Include(lr => lr.Patient)
                .OrderByDescending(lr => lr.DateCollected)
                .ThenBy(lr => lr.TestName)
                .Select(lr => new
                {
                    lr.LabResultId, lr.AdmissionId, lr.TestName,
                    lr.ResultValue, lr.Unit, lr.ReferenceRange,
                    lr.DateCollected, lr.CriticalFlag,
                    PatientName = lr.Patient!.FirstName + " " + lr.Patient.LastName
                })
                .ToListAsync();

            if (labs.Count == 0)
                return $"No lab results found for patient ID {patientId}.";

            var lines = labs.Select(lr =>
                $"  - [{lr.DateCollected:yyyy-MM-dd}] Lab#{lr.LabResultId} (Admission#{lr.AdmissionId?.ToString() ?? "N/A"}): " +
                $"{lr.TestName} = {lr.ResultValue} {lr.Unit} " +
                $"(ref: {lr.ReferenceRange}){(lr.CriticalFlag ? " ⚠ CRITICAL" : "")}");

            return $"Lab history for patient {labs[0].PatientName} (ID {patientId}) — {labs.Count} result(s):\n" +
                   string.Join("\n", lines);
        }

        [Description("Returns all lab results flagged as critical. " +
                     "Optionally filter to a specific patient by providing their patient ID. " +
                     "Omit patientId (or pass 0) to return critical labs across all patients. " +
                     "Results are ordered by collection date descending.")]
        private async Task<string> GetPatientsWithCriticalLabs(
            [Description("Optional patient ID to filter by. Pass 0 to return critical labs for all patients.")] int patientId = 0)
        {
            if (patientId > 0)
            {
                var exists = await _db.Patients.AsNoTracking().AnyAsync(p => p.PatientId == patientId);
                if (!exists)
                    return $"Patient with ID {patientId} does not exist.";
            }

            var query = _db.LabResults
                .AsNoTracking()
                .Where(lr => lr.CriticalFlag && (patientId == 0 || lr.PatientId == patientId))
                .Include(lr => lr.Patient);

            var labs = await query
                .OrderByDescending(lr => lr.DateCollected)
                .ThenBy(lr => lr.Patient!.LastName)
                .ThenBy(lr => lr.Patient!.FirstName)
                .Select(lr => new
                {
                    lr.LabResultId, lr.PatientId, lr.AdmissionId,
                    PatientName = lr.Patient!.FirstName + " " + lr.Patient.LastName,
                    lr.TestName, lr.ResultValue, lr.Unit,
                    lr.ReferenceRange, lr.DateCollected
                })
                .ToListAsync();

            if (labs.Count == 0)
                return "No critical lab results found.";

            var lines = labs.Select(lr =>
                $"  - [{lr.DateCollected:yyyy-MM-dd}] Patient {lr.PatientName} (ID {lr.PatientId}), " +
                $"Lab#{lr.LabResultId} (Admission#{lr.AdmissionId?.ToString() ?? "N/A"}): " +
                $"{lr.TestName} = {lr.ResultValue} {lr.Unit} (ref: {lr.ReferenceRange})");

            var scope = patientId > 0 ? $"patient ID {patientId}" : "all patients";
            return $"Found {labs.Count} critical lab result(s) for {scope}:\n" + string.Join("\n", lines);
        }

        [Description("Returns all patients who are currently admitted (discharge date is null), " +
                     "along with their admission details and attending physician. " +
                     "Results are ordered by department then bed number.")]
        private async Task<string> GetCurrentlyAdmittedPatients()
        {
            var admissions = await _db.Admissions
                .AsNoTracking()
                .Where(a => a.DischargeDate == default)
                .Include(a => a.Patient)
                .Include(a => a.Physician)
                .OrderBy(a => a.Department)
                .ThenBy(a => a.BedNumber)
                .Select(a => new
                {
                    a.AdmissionId, a.AdmissionDate, a.Department, a.BedNumber, a.ReasonForVisit,
                    PatientName  = a.Patient!.FirstName + " " + a.Patient.LastName,
                    PatientId    = a.Patient.PatientId,
                    PhysicianName = a.Physician != null
                                    ? a.Physician.FirstName + " " + a.Physician.LastName
                                    : "N/A"
                })
                .ToListAsync();

            if (admissions.Count == 0)
                return "No currently admitted patients found.";

            var lines = admissions.Select(a =>
                $"  - Admission#{a.AdmissionId} | {a.Department}, Bed {a.BedNumber} | " +
                $"Patient: {a.PatientName} (ID {a.PatientId}) | " +
                $"Admitted: {a.AdmissionDate:yyyy-MM-dd} | " +
                $"Physician: {a.PhysicianName} | Reason: {a.ReasonForVisit}");

            return $"Currently admitted patients ({admissions.Count}):\n" + string.Join("\n", lines);
        }

        [Description("Returns all patients who have had an admission under a specific physician, " +
                     "along with admission details. Results are ordered by patient last name then first name.")]
        private async Task<string> GetPatientsByPhysician(
            [Description("The physician ID to look up.")] int physicianId)
        {
            var physicianExists = await _db.Physicians.AsNoTracking().AnyAsync(ph => ph.PhysicianId == physicianId);
            if (!physicianExists)
                return $"Physician with ID {physicianId} does not exist.";

            var admissions = await _db.Admissions
                .AsNoTracking()
                .Where(a => a.PhysicianId == physicianId)
                .Include(a => a.Patient)
                .Include(a => a.Physician)
                .OrderBy(a => a.Patient!.LastName)
                .ThenBy(a => a.Patient!.FirstName)
                .Select(a => new
                {
                    PhysicianName = a.Physician!.FirstName + " " + a.Physician.LastName,
                    a.Physician.Specialty,
                    PatientId    = a.Patient!.PatientId,
                    PatientName  = a.Patient.FirstName + " " + a.Patient.LastName,
                    a.Patient.DateOfBirth, a.Patient.Gender,
                    a.AdmissionId, a.AdmissionDate, a.DischargeDate,
                    a.Department, a.ReasonForVisit
                })
                .ToListAsync();

            if (admissions.Count == 0)
                return $"No patients found for physician ID {physicianId}.";

            var physicianName = admissions[0].PhysicianName;
            var lines = admissions.Select(a =>
                $"  - Patient: {a.PatientName} (ID {a.PatientId}, {a.Gender ?? "N/A"}, " +
                $"DOB: {a.DateOfBirth?.ToString("yyyy-MM-dd") ?? "N/A"}) | " +
                $"Admission#{a.AdmissionId}: {a.AdmissionDate:yyyy-MM-dd} → " +
                $"{a.DischargeDate:yyyy-MM-dd} | Dept: {a.Department} | {a.ReasonForVisit}");

            return $"Patients seen by Dr. {physicianName} (ID {physicianId}) — {admissions.Count} admission(s):\n" +
                   string.Join("\n", lines);
        }

        [Description("Returns a full summary for a specific patient including demographics, medical history, " +
                     "current medications, and admission records.")]
        private async Task<string> GetPatientSummary(
            [Description("The patient ID to summarise.")] int patientId)
        {
            var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.PatientId == patientId);
            if (patient == null)
                return $"Patient with ID {patientId} does not exist.";

            // Demographics + medical history
            var mh = await _db.MedicalHistories.AsNoTracking().FirstOrDefaultAsync(m => m.PatientId == patientId);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Patient Summary: {patient.FirstName} {patient.LastName} (ID {patientId}) ===");
            sb.AppendLine();
            sb.AppendLine("-- Demographics & Medical History --");
            sb.AppendLine($"  DOB: {patient.DateOfBirth?.ToString("yyyy-MM-dd") ?? "N/A"} | " +
                          $"Gender: {patient.Gender ?? "N/A"} | Phone: {patient.PhoneNumber ?? "N/A"}");
            sb.AppendLine($"  Emergency contact: {patient.EmergencyContactName ?? "N/A"} ({patient.EmergencyContactPhone ?? "N/A"})");
            if (mh != null)
            {
                sb.AppendLine($"  Chronic conditions: {mh.ChronicConditions ?? "None on record"}");
                sb.AppendLine($"  Allergies: {mh.Allergies ?? "None on record"}");
                sb.AppendLine($"  Prior surgeries: {mh.PriorSurgeries ?? "None on record"}");
                sb.AppendLine($"  Family history: {mh.FamilyHistory ?? "None on record"}");
                if (!string.IsNullOrWhiteSpace(mh.Notes))
                    sb.AppendLine($"  Notes: {mh.Notes}");
            }
            else
            {
                sb.AppendLine("  No medical history on record.");
            }

            // Medications
            var meds = await _db.Medications
                .AsNoTracking()
                .Where(m => m.PatientId == patientId)
                .Include(m => m.Physician)
                .OrderByDescending(m => m.StartDate)
                .Select(m => new
                {
                    m.MedicationId, m.DrugName, m.Dose, m.Frequency,
                    m.StartDate, m.EndDate,
                    PrescribedBy = m.Physician != null ? m.Physician.FirstName + " " + m.Physician.LastName : "N/A"
                })
                .ToListAsync();

            sb.AppendLine();
            sb.AppendLine("-- Medications --");
            if (meds.Count == 0)
            {
                sb.AppendLine("  No medication records found.");
            }
            else
            {
                foreach (var m in meds)
                    sb.AppendLine($"  - #{m.MedicationId} {m.DrugName} {m.Dose} {m.Frequency} | " +
                                  $"{m.StartDate:yyyy-MM-dd} → {m.EndDate?.ToString("yyyy-MM-dd") ?? "ongoing"} | " +
                                  $"Prescribed by: Dr. {m.PrescribedBy}");
            }

            // Admissions
            var admissions = await _db.Admissions
                .AsNoTracking()
                .Where(a => a.PatientId == patientId)
                .Include(a => a.Physician)
                .OrderByDescending(a => a.AdmissionDate)
                .Select(a => new
                {
                    a.AdmissionId, a.AdmissionDate, a.DischargeDate,
                    a.Department, a.BedNumber, a.ReasonForVisit,
                    PhysicianName = a.Physician != null ? a.Physician.FirstName + " " + a.Physician.LastName : "N/A"
                })
                .ToListAsync();

            sb.AppendLine();
            sb.AppendLine("-- Admissions --");
            if (admissions.Count == 0)
            {
                sb.AppendLine("  No admission records found.");
            }
            else
            {
                foreach (var a in admissions)
                    sb.AppendLine($"  - #{a.AdmissionId} {a.AdmissionDate:yyyy-MM-dd} → " +
                                  $"{a.DischargeDate:yyyy-MM-dd} | " +
                                  $"Dept: {a.Department}, Bed {a.BedNumber} | " +
                                  $"Attending: Dr. {a.PhysicianName} | {a.ReasonForVisit}");
            }

            return sb.ToString();
        }
    }
}
