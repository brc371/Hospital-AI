using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Hospital_AI.Data;
using Hospital_AI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace Hospital_AI.Services
{
    /// <summary>
    /// Generates SOAP notes by streaming responses from the configured <see cref="IChatClient"/>.
    /// A tool function is exposed to the model so it can look up the patient's prior saved note
    /// versions on demand, injecting relevant history for returning patients without always
    /// sending the full history up front.
    /// </summary>
    public class NoteGenerationService : INoteGenerationService
    {
        private readonly IChatClient _chatClient;
        private readonly ClinicalScribeDbContext _dbContext;

        /// <summary>Initializes a new instance of <see cref="NoteGenerationService"/>.</summary>
        /// <param name="chatClient">The AI chat client used to generate note text.</param>
        /// <param name="dbContext">The database context used to load encounter/patient data.</param>
        public NoteGenerationService(IChatClient chatClient, ClinicalScribeDbContext dbContext)
        {
            _chatClient = chatClient;
            _dbContext = dbContext;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<string> GenerateNoteStreamAsync(Guid encounterId, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var encounter = await _dbContext.Encounters
                .Include(e => e.Patient)
                .FirstOrDefaultAsync(e => e.Id == encounterId, cancellationToken);

            if (encounter is null || encounter.Patient is null)
            {
                yield break;
            }

            var patientId = encounter.PatientId;

            // Tool the model can call to retrieve this patient's prior saved SOAP notes for
            // context. Bound via closure to this encounter's patient so the model cannot query
            // other patients' history.
            [Description("Gets the patient's prior saved SOAP note versions from previous encounters, most recent first. Use this to reference relevant history for a returning patient.")]
            async Task<string> GetPatientHistoryAsync()
            {
                var priorVersions = await _dbContext.NoteVersions
                    .Include(v => v.Encounter)
                    .Where(v => v.Encounter != null && v.Encounter.PatientId == patientId && v.EncounterId != encounterId)
                    .OrderByDescending(v => v.SavedAtUtc)
                    .Take(5)
                    .ToListAsync(cancellationToken);

                if (priorVersions.Count == 0)
                {
                    return "No prior notes found for this patient.";
                }

                var sb = new StringBuilder();
                foreach (var version in priorVersions)
                {
                    sb.AppendLine($"--- Note from {version.SavedAtUtc:yyyy-MM-dd} ---");
                    sb.AppendLine($"Subjective: {version.Subjective}");
                    sb.AppendLine($"Objective: {version.Objective}");
                    sb.AppendLine($"Assessment: {version.Assessment}");
                    sb.AppendLine($"Plan: {version.Plan}");
                }

                return sb.ToString();
            }

            var systemPrompt =
                "You are a clinical documentation assistant. Generate a structured SOAP note " +
                "(Subjective, Objective, Assessment, Plan) from the provider's raw transcript/" +
                "observations below. Use the get_patient_history tool if the transcript suggests " +
                "this is a returning patient and prior context would improve the note. Write in " +
                "clear, professional clinical language. Format the output with 'Subjective:', " +
                "'Objective:', 'Assessment:', and 'Plan:' section headers.";

            var patientContext =
                $"Patient: {encounter.Patient.FirstName} {encounter.Patient.LastName}, " +
                $"DOB: {encounter.Patient.DateOfBirth:yyyy-MM-dd}\n\n" +
                $"Transcript/clinical observations:\n{encounter.TranscriptText}";

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, patientContext)
            };

            var chatOptions = new ChatOptions
            {
                Tools = [AIFunctionFactory.Create(GetPatientHistoryAsync, name: "get_patient_history")]
            };

            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, chatOptions, cancellationToken))
            {
                if (!string.IsNullOrEmpty(update.Text))
                {
                    yield return update.Text;
                }
            }
        }
    }
}
