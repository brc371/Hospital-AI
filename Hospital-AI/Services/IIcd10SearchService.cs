using Hospital_AI.Models;

namespace Hospital_AI.Services
{
    /// <summary>
    /// Searches the embedded ICD-10 code reference list for codes/descriptions relevant to a
    /// free-text query (e.g. "diabetes", "chest pain", "J45"). Search runs entirely against the
    /// locally seeded <see cref="Icd10Code"/> table - no external ICD-10 API is called.
    /// </summary>
    public interface IIcd10SearchService
    {
        /// <summary>
        /// Searches ICD-10 codes by relevance to the given free-text query, matching against
        /// both the code and its description. Returns an empty list for an empty/whitespace
        /// query.
        /// </summary>
        /// <param name="query">The provider's free-text search input.</param>
        /// <param name="maxResults">The maximum number of results to return.</param>
        Task<List<Icd10Code>> SearchAsync(string query, int maxResults = 20);
    }
}
