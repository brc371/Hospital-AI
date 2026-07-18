using Hospital_AI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital_AI.Controllers
{
    /// <summary>
    /// Provides ICD-10 code search for the encounter workspace's code search widget. Searches
    /// only the embedded, locally seeded ICD-10 reference list - no external API is called.
    /// </summary>
    [ApiController]
    [Route("api/icd10codes")]
    [Authorize]
    public class Icd10CodesController : ControllerBase
    {
        private readonly IIcd10SearchService _icd10SearchService;

        /// <summary>Initializes a new instance of <see cref="Icd10CodesController"/>.</summary>
        public Icd10CodesController(IIcd10SearchService icd10SearchService)
        {
            _icd10SearchService = icd10SearchService;
        }

        /// <summary>
        /// Searches ICD-10 codes by free-text query (matching code or description).
        /// </summary>
        /// <param name="q">The free-text search query.</param>
        [HttpGet("search")]
        public async Task<IActionResult> SearchAsync([FromQuery] string q)
        {
            var results = await _icd10SearchService.SearchAsync(q);
            return Ok(results.Select(c => new { c.Code, c.Description }));
        }
    }
}
