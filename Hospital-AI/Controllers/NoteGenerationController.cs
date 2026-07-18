using Hospital_AI.Models;
using Hospital_AI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital_AI.Controllers
{
    /// <summary>
    /// Streams AI-generated SOAP note text for an encounter using Server-Sent Events (SSE), so
    /// the encounter workspace UI can display generation token-by-token as it happens.
    /// </summary>
    [ApiController]
    [Route("api/encounters/{encounterId:guid}/generate-note")]
    [Authorize]
    public class NoteGenerationController : ControllerBase
    {
        private readonly INoteGenerationService _noteGenerationService;
        private readonly IEncounterService _encounterService;
        private readonly IRoleResolutionService _roleResolutionService;

        /// <summary>Initializes a new instance of <see cref="NoteGenerationController"/>.</summary>
        public NoteGenerationController(
            INoteGenerationService noteGenerationService,
            IEncounterService encounterService,
            IRoleResolutionService roleResolutionService)
        {
            _noteGenerationService = noteGenerationService;
            _encounterService = encounterService;
            _roleResolutionService = roleResolutionService;
        }

        /// <summary>
        /// Streams the generated SOAP note text for the given encounter as Server-Sent Events.
        /// Only the owning provider may generate a note for their own encounter.
        /// </summary>
        /// <param name="encounterId">The encounter to generate a note for.</param>
        /// <param name="cancellationToken">A token that is canceled if the client disconnects.</param>
        [HttpGet]
        public async Task GetAsync(Guid encounterId, CancellationToken cancellationToken)
        {
            var email = User.Identity?.Name;
            var provider = email is null ? null : await _roleResolutionService.ResolveByEmailAsync(email);

            if (provider is null)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            // Only the owning provider may generate a note (Admins can view but not generate on
            // another provider's behalf).
            var encounter = await _encounterService.GetEncounterAsync(encounterId, provider.Id, isAdmin: false);
            if (encounter is null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";

            try
            {
                await foreach (var chunk in _noteGenerationService.GenerateNoteStreamAsync(encounterId, cancellationToken))
                {
                    // SSE "data:" lines cannot contain raw newlines; encode them so the client
                    // can decode back to the original text with embedded line breaks.
                    var encodedChunk = chunk.Replace("\n", "\\n");
                    await Response.WriteAsync($"data: {encodedChunk}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }

                await Response.WriteAsync("event: done\ndata: end\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Client disconnected/navigated away; nothing further to do.
            }
        }
    }
}
