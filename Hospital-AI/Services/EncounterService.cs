using Hospital_AI.Data;
using Hospital_AI.Models;
using Microsoft.EntityFrameworkCore;

namespace Hospital_AI.Services
{
    /// <summary>
    /// Manages encounter lifecycle: starting new encounters (matching/creating patients by
    /// first/last/DOB) and persisting in-progress draft note text.
    /// </summary>
    public class EncounterService : IEncounterService
    {
        private readonly ClinicalScribeDbContext _dbContext;
        private readonly IPatientMatchingService _patientMatchingService;

        /// <summary>Initializes a new instance of <see cref="EncounterService"/>.</summary>
        /// <param name="dbContext">The database context used to read/write encounters.</param>
        /// <param name="patientMatchingService">The service used to match/create patients.</param>
        public EncounterService(ClinicalScribeDbContext dbContext, IPatientMatchingService patientMatchingService)
        {
            _dbContext = dbContext;
            _patientMatchingService = patientMatchingService;
        }

        /// <inheritdoc />
        public async Task<Encounter> StartEncounterAsync(Guid providerId, string firstName, string lastName, DateOnly dateOfBirth, Guid? noteTemplateId = null)
        {
            var patient = await _patientMatchingService.FindOrCreateAsync(firstName, lastName, dateOfBirth);

            var encounter = new Encounter
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                ProviderId = providerId,
                NoteTemplateId = noteTemplateId,
                TranscriptText = string.Empty,
                Status = EncounterStatus.Draft
            };

            _dbContext.Encounters.Add(encounter);
            await _dbContext.SaveChangesAsync();

            encounter.Patient = patient;
            return encounter;
        }

        /// <inheritdoc />
        public async Task<Encounter?> SaveDraftAsync(Guid encounterId, Guid providerId, string transcriptText, string? draftNoteText)
        {
            var encounter = await _dbContext.Encounters
                .FirstOrDefaultAsync(e => e.Id == encounterId && e.ProviderId == providerId);

            if (encounter is null)
            {
                return null;
            }

            // Razor Pages model binding converts a submitted empty string to null by default
            // (legacy ConvertEmptyStringToNull behavior), which would otherwise violate the
            // NOT NULL constraint on Encounters.TranscriptText when a provider clears the
            // transcript box entirely (e.g. to start typing a new observation).
            encounter.TranscriptText = transcriptText ?? string.Empty;
            encounter.DraftNoteText = draftNoteText;
            encounter.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync();

            return encounter;
        }

        /// <inheritdoc />
        public async Task<Encounter?> GetEncounterAsync(Guid encounterId, Guid providerId, bool isAdmin)
        {
            var query = _dbContext.Encounters
                .Include(e => e.Patient)
                .Where(e => e.Id == encounterId);

            if (!isAdmin)
            {
                query = query.Where(e => e.ProviderId == providerId);
            }

            return await query.FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<List<Encounter>> GetEncountersForProviderAsync(Guid providerId)
        {
            return await _dbContext.Encounters
                .Include(e => e.Patient)
                .Where(e => e.ProviderId == providerId)
                .OrderByDescending(e => e.UpdatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<Encounter>> GetAllEncountersAsync()
        {
            return await _dbContext.Encounters
                .Include(e => e.Patient)
                .Include(e => e.Provider)
                .OrderByDescending(e => e.UpdatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<NoteVersion?> SaveNoteVersionAsync(Guid encounterId, Guid providerId)
        {
            var encounter = await _dbContext.Encounters
                .FirstOrDefaultAsync(e => e.Id == encounterId && e.ProviderId == providerId);

            if (encounter is null || string.IsNullOrWhiteSpace(encounter.DraftNoteText))
            {
                return null;
            }

            var (subjective, objective, assessment, plan) = SoapNoteParser.Parse(encounter.DraftNoteText);

            var lastVersionNumber = await _dbContext.NoteVersions
                .Where(v => v.EncounterId == encounterId)
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => (int?)v.VersionNumber)
                .FirstOrDefaultAsync();

            var noteVersion = new NoteVersion
            {
                Id = Guid.NewGuid(),
                EncounterId = encounterId,
                VersionNumber = (lastVersionNumber ?? 0) + 1,
                Subjective = subjective,
                Objective = objective,
                Assessment = assessment,
                Plan = plan,
                SavedByProviderId = providerId
            };

            _dbContext.NoteVersions.Add(noteVersion);

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = nameof(NoteVersion),
                EntityId = noteVersion.Id,
                Action = "Created",
                PerformedByProviderId = providerId,
                Details = $"Saved version {noteVersion.VersionNumber} for encounter {encounterId}."
            });

            encounter.Status = EncounterStatus.Saved;
            encounter.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync();

            return noteVersion;
        }

        /// <inheritdoc />
        public async Task<List<NoteVersion>> GetNoteVersionsAsync(Guid encounterId)
        {
            return await _dbContext.NoteVersions
                .Include(v => v.SavedByProvider)
                .Where(v => v.EncounterId == encounterId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();
        }
    }
}
