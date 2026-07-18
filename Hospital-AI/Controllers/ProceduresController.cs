using Hospital_AI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hospital_AI.Data;

namespace Hospital_AI.Controllers
{
    /// <summary>
    /// Controller for managing procedures in the Hospital application
    /// </summary>
    [ApiController]
    [Route("patients")]
    [Produces("application/json")]
    public class ProceduresController : ControllerBase
    {
        /// <summary>The EF Core database context for patient persistence.</summary>
        private readonly MyDataBaseContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProceduresController"/> class.
        /// </summary>
        /// <param name="dbContext">The EF Core database context.</param>
        public ProceduresController(MyDataBaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Retrieves all patients, optionally filtered by patient ID.
        /// Mirrors the validation and error-handling behaviour of the stored procedures:
        /// returns 400 for an invalid ID, 404 when the specified patient does not exist,
        /// and a friendly message when no patient records are found at all.
        /// </summary>
        /// <param name="patient_id">Optional. When provided, must be a positive integer. Returns only the patient with this ID.</param>
        /// <returns>A list of patients, or a friendly message when none exist.</returns>
        /// <response code="200">OK – Returns the list of patients, or a friendly message when none are found.</response>
        /// <response code="400">Bad Request – The supplied patient_id is not a valid positive integer.</response>
        /// <response code="404">Not Found – No patient with the given ID exists.</response>
        [HttpGet(Name = "GetAllProcedures")]
        [ProducesResponseType(typeof(IEnumerable<PatientResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<PatientResponse>>> GetAllProcedures([FromQuery] int? patient_id = null)
        {
            // Mirror SP validation: reject invalid (non-positive) IDs when one is supplied
            if (patient_id.HasValue && patient_id.Value <= 0)
                return BadRequest(new { message = "Invalid patient ID. Please provide a valid positive patient_id." });

            // When a specific ID is requested, verify the patient exists before querying
            if (patient_id.HasValue)
            {
                var exists = await _dbContext.Patients
                    .AsNoTracking()
                    .AnyAsync(p => p.PatientId == patient_id.Value);

                if (!exists)
                    return NotFound(new { message = $"Patient with ID {patient_id.Value} does not exist." });
            }

            var query = _dbContext.Patients.AsNoTracking();

            if (patient_id.HasValue)
                query = query.Where(p => p.PatientId == patient_id.Value);

            var patients = await query
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();

            // Mirror SP friendly-message behaviour when no records exist
            if (patients.Count == 0)
                return Ok(new { message = "No patient records found." });

            var result = patients.Select(p => new PatientResponse
            {
                PatientId             = p.PatientId,
                FirstName             = p.FirstName,
                LastName              = p.LastName,
                Gender                = p.Gender,
                PhoneNumber           = p.PhoneNumber,
                Address               = p.Address,
                EmergencyContactName  = p.EmergencyContactName,
                EmergencyContactPhone = p.EmergencyContactPhone,
                DateOfBirth           = p.DateOfBirth,
                NationalId            = p.NationalId,
                CreatedAt             = p.CreatedAt,
                UpdatedAt             = p.UpdatedAt
            });

            return Ok(result);
        }
            /// <summary>
            /// Retrieves the complete lab history for a specific patient (mirrors GetPatientLabHistory stored procedure).
            /// Results are ordered by collection date descending, then by test name.
            /// </summary>
            /// <param name="patient_id">The ID of the patient whose lab history to retrieve.</param>
            /// <returns>A list of lab results for the patient, ordered by most recent first.</returns>
            /// <response code="200">OK – Returns the lab history; an empty array when no results exist.</response>
            /// <response code="404">Not Found – No patient with the given ID exists.</response>
            [HttpGet("{patient_id}/labs", Name = "GetPatientLabHistory")]
            [ProducesResponseType(typeof(IEnumerable<LabResultResponse>), StatusCodes.Status200OK)]
            [ProducesResponseType(StatusCodes.Status404NotFound)]
            public async Task<ActionResult<IEnumerable<LabResultResponse>>> GetPatientLabHistory(int patient_id)
            {
                var patientExists = await _dbContext.Patients
                    .AsNoTracking()
                    .AnyAsync(p => p.PatientId == patient_id);

                if (!patientExists)
                    return NotFound(new { message = $"Patient with ID {patient_id} does not exist." });

                var results = await _dbContext.LabResults
                    .AsNoTracking()
                    .Where(lr => lr.PatientId == patient_id)
                    .Include(lr => lr.Patient)
                    .OrderByDescending(lr => lr.DateCollected)
                    .ThenBy(lr => lr.TestName)
                    .Select(lr => new LabResultResponse
                    {
                        LabResultId    = lr.LabResultId,
                        PatientId      = lr.PatientId,
                        FirstName      = lr.Patient!.FirstName,
                        LastName       = lr.Patient.LastName,
                        AdmissionId    = lr.AdmissionId,
                        TestName       = lr.TestName,
                        ResultValue    = lr.ResultValue,
                        Unit           = lr.Unit,
                        ReferenceRange = lr.ReferenceRange,
                        DateCollected  = lr.DateCollected,
                        CriticalFlag   = lr.CriticalFlag
                    })
                    .ToListAsync();

                return Ok(results);
            }

        /// <summary>
        /// Retrieves a multi-section summary for a specific patient
        /// (mirrors GetPatientSummary stored procedure).
        /// Returns patient demographics and medical history, medication records,
        /// and admission records. Friendly messages are returned when no medications
        /// or admissions are found.
        /// </summary>
        /// <param name="patient_id">The ID of the patient to summarize.</param>
        /// <returns>A summary object containing demographics, medications, and admissions.</returns>
        /// <response code="200">OK – Returns the patient summary.</response>
        /// <response code="404">Not Found – No patient with the given ID exists.</response>
        [HttpGet("{patient_id}/summary", Name = "GetPatientSummary")]
        [ProducesResponseType(typeof(PatientSummaryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PatientSummaryResponse>> GetPatientSummary(int patient_id)
        {
            var patient = await _dbContext.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PatientId == patient_id);

            if (patient == null)
                return NotFound(new { message = $"Patient with ID {patient_id} does not exist." });

            // Result set 1: Demographics + medical history (LEFT JOIN — history may be null)
            var medicalHistory = await _dbContext.MedicalHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(mh => mh.PatientId == patient_id);

            var demographics = new PatientSummaryDemographics
            {
                PatientId              = patient.PatientId,
                FirstName              = patient.FirstName,
                LastName               = patient.LastName,
                DateOfBirth            = patient.DateOfBirth,
                Gender                 = patient.Gender,
                PhoneNumber            = patient.PhoneNumber,
                EmergencyContactName   = patient.EmergencyContactName,
                EmergencyContactPhone  = patient.EmergencyContactPhone,
                ChronicConditions      = medicalHistory?.ChronicConditions,
                PriorSurgeries         = medicalHistory?.PriorSurgeries,
                Allergies              = medicalHistory?.Allergies,
                FamilyHistory          = medicalHistory?.FamilyHistory,
                Notes                  = medicalHistory?.Notes
            };

            // Result set 2: Medications ordered by start_date DESC
            var medicationRecords = await _dbContext.Medications
                .AsNoTracking()
                .Where(m => m.PatientId == patient_id)
                .Include(m => m.Physician)
                .OrderByDescending(m => m.StartDate)
                .Select(m => new PatientSummaryMedicationItem
                {
                    MedicationId                   = m.MedicationId,
                    DrugName                       = m.DrugName,
                    Dose                           = m.Dose,
                    Frequency                      = m.Frequency,
                    StartDate                      = m.StartDate,
                    EndDate                        = m.EndDate,
                    PrescribingPhysicianFirstName  = m.Physician != null ? m.Physician.FirstName : null,
                    PrescribingPhysicianLastName   = m.Physician != null ? m.Physician.LastName  : null
                })
                .ToListAsync();

            var medications = new PatientSummaryMedications();
            if (medicationRecords.Count == 0)
                medications.Message = "No medication records found for this patient.";
            else
                medications.Records = medicationRecords;

            // Result set 3: Admissions ordered by admission_date DESC
            var admissionRecords = await _dbContext.Admissions
                .AsNoTracking()
                .Where(a => a.PatientId == patient_id)
                .Include(a => a.Physician)
                .OrderByDescending(a => a.AdmissionDate)
                .Select(a => new PatientSummaryAdmissionItem
                {
                    AdmissionId                  = a.AdmissionId,
                    AdmissionDate                = a.AdmissionDate,
                    DischargeDate                = a.DischargeDate,
                    Department                   = a.Department,
                    BedNumber                    = a.BedNumber,
                    ReasonForVisit               = a.ReasonForVisit,
                    AttendingPhysicianFirstName  = a.Physician != null ? a.Physician.FirstName : null,
                    AttendingPhysicianLastName   = a.Physician != null ? a.Physician.LastName  : null
                })
                .ToListAsync();

            var admissions = new PatientSummaryAdmissions();
            if (admissionRecords.Count == 0)
                admissions.Message = "No admission records found for this patient.";
            else
                admissions.Records = admissionRecords;

            return Ok(new PatientSummaryResponse
            {
                Demographics = demographics,
                Medications  = medications,
                Admissions   = admissions
            });
        }

        /// <summary>
        /// Retrieves all patients assigned to a specific physician through the Admissions table
        /// (mirrors GetPatientsByPhysician stored procedure).
        /// Returns physician information, patient demographics, admission details, department,
        /// and reason for visit. Results are ordered by admission date descending.
        /// </summary>
        /// <param name="physician_id">The ID of the physician whose patients to retrieve.</param>
        /// <returns>A list of patient/admission records for the physician.</returns>
        /// <response code="200">OK – Returns the patient list; an empty array when no patients are assigned.</response>
        /// <response code="404">Not Found – No physician with the given ID exists.</response>
        [HttpGet("~/physicians/{physician_id}/patients", Name = "GetPatientsByPhysician")]
        [ProducesResponseType(typeof(IEnumerable<PatientsByPhysicianResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<PatientsByPhysicianResponse>>> GetPatientsByPhysician(int physician_id)
        {
            var physicianExists = await _dbContext.Physicians
                .AsNoTracking()
                .AnyAsync(ph => ph.PhysicianId == physician_id);

            if (!physicianExists)
                return NotFound(new { message = $"Physician with ID {physician_id} does not exist." });

            var results = await _dbContext.Admissions
                .AsNoTracking()
                .Where(a => a.PhysicianId == physician_id)
                .Include(a => a.Physician)
                .Include(a => a.Patient)
                .OrderByDescending(a => a.AdmissionDate)
                .Select(a => new PatientsByPhysicianResponse
                {
                    PhysicianId       = a.Physician!.PhysicianId,
                    PhysicianFirstName = a.Physician.FirstName,
                    PhysicianLastName  = a.Physician.LastName,
                    Specialization    = a.Physician.Specialty,
                    PatientId         = a.Patient!.PatientId,
                    PatientFirstName  = a.Patient.FirstName,
                    PatientLastName   = a.Patient.LastName,
                    Gender            = a.Patient.Gender,
                    DateOfBirth       = a.Patient.DateOfBirth,
                    PhoneNumber       = a.Patient.PhoneNumber,
                    AdmissionId       = a.AdmissionId,
                    AdmissionDate     = a.AdmissionDate,
                    DischargeDate     = a.DischargeDate,
                    ReasonForVisit    = a.ReasonForVisit,
                    DepartmentName    = a.Department
                })
                .ToListAsync();

            if (results.Count == 0)
                return Ok(new { message = $"Physician with ID {physician_id} has no patients assigned." });

            return Ok(results);
        }

        /// <summary>
        /// Searches for patients by first name, last name, or full name
        /// (mirrors SearchPatientsByName stored procedure).
        /// The search is case-insensitive and matches any part of the name.
        /// Returns a friendly message when no patients match the search text.
        /// </summary>
        /// <param name="name">The search text to match against first name, last name, or full name. Must be at least 2 characters.</param>
        /// <returns>A list of matching patients, or a friendly message when none are found.</returns>
        /// <response code="200">OK – Returns matching patients, or a friendly message when none are found.</response>
        /// <response code="400">Bad Request – The search text is empty, whitespace-only, or fewer than 2 characters.</response>
        [HttpGet("search", Name = "SearchPatientsByName")]
        [ProducesResponseType(typeof(IEnumerable<PatientSearchResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<PatientSearchResponse>>> SearchPatientsByName([FromQuery] string? name)
        {
            // Mirror SP validation: reject null/empty search text
            var searchText = name?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(searchText))
                return BadRequest(new { message = "Search text cannot be empty. Please enter a first name, last name, or full name." });

            // Mirror SP validation: require at least 2 characters
            if (searchText.Length < 2)
                return BadRequest(new { message = "Search text must be at least 2 characters long." });

            var results = await _dbContext.Patients
                .AsNoTracking()
                .Where(p => p.FirstName.Contains(searchText) ||
                            p.LastName.Contains(searchText)  ||
                            (p.FirstName + " " + p.LastName).Contains(searchText))
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Select(p => new PatientSearchResponse
                {
                    PatientId   = p.PatientId,
                    FirstName   = p.FirstName,
                    LastName    = p.LastName,
                    DateOfBirth = p.DateOfBirth,
                    Gender      = p.Gender,
                    PhoneNumber = p.PhoneNumber
                })
                .ToListAsync();

            // Mirror SP friendly-message behaviour when no patients match
            if (results.Count == 0)
                return Ok(new { message = $"No patients found matching '{searchText}'." });

            return Ok(results);
        }

        /// <summary>
        /// Returns all lab results flagged as critical, optionally filtered to a single patient
        /// (mirrors GetPatientsWithCriticalLabs stored procedure).
        /// When patient_id is omitted, all patients with critical labs are returned.
        /// When patient_id is supplied the patient must exist; a 404 is returned otherwise.
        /// A friendly message is returned when no critical results are found.
        /// </summary>
        /// <param name="patient_id">Optional. When provided, must be a positive integer. Filters results to this patient only.</param>
        /// <returns>A list of critical lab results, or a friendly message when none exist.</returns>
        /// <response code="200">OK – Returns critical lab results, or a friendly message when none are found.</response>
        /// <response code="400">Bad Request – The supplied patient_id is not a valid positive integer.</response>
        /// <response code="404">Not Found – No patient with the given ID exists.</response>
        [HttpGet("critical-labs", Name = "GetPatientsWithCriticalLabs")]
        [ProducesResponseType(typeof(IEnumerable<LabResultResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<LabResultResponse>>> GetPatientsWithCriticalLabs([FromQuery] int? patient_id = null)
        {
            // Mirror SP validation: reject invalid (non-positive) IDs when one is supplied
            if (patient_id.HasValue && patient_id.Value <= 0)
                return BadRequest(new { message = "Invalid patient ID. Please provide a valid positive patient_id." });

            // Mirror SP: validate patient exists only when an ID is provided
            if (patient_id.HasValue)
            {
                var exists = await _dbContext.Patients
                    .AsNoTracking()
                    .AnyAsync(p => p.PatientId == patient_id.Value);

                if (!exists)
                    return NotFound(new { message = $"Patient with ID {patient_id.Value} does not exist." });
            }

            var query = _dbContext.LabResults
                .AsNoTracking()
                .Where(lr => lr.CriticalFlag &&
                             (!patient_id.HasValue || lr.PatientId == patient_id.Value))
                .Include(lr => lr.Patient);

            var results = await query
                .OrderByDescending(lr => lr.DateCollected)
                .ThenBy(lr => lr.Patient!.LastName)
                .ThenBy(lr => lr.Patient!.FirstName)
                .Select(lr => new LabResultResponse
                {
                    LabResultId    = lr.LabResultId,
                    PatientId      = lr.PatientId,
                    FirstName      = lr.Patient!.FirstName,
                    LastName       = lr.Patient.LastName,
                    AdmissionId    = lr.AdmissionId,
                    TestName       = lr.TestName,
                    ResultValue    = lr.ResultValue,
                    Unit           = lr.Unit,
                    ReferenceRange = lr.ReferenceRange,
                    DateCollected  = lr.DateCollected,
                    CriticalFlag   = lr.CriticalFlag
                })
                .ToListAsync();

            // Mirror SP friendly-message behaviour when no critical labs exist
            if (results.Count == 0)
                return Ok(new { message = "No critical lab results found." });

            return Ok(results);
        }
        }
    }

