using Hospital_AI.Data;
using Hospital_AI.Models;
using Microsoft.EntityFrameworkCore;

namespace Hospital_AI.Services
{
    /// <summary>
    /// Searches the embedded ICD-10 code list using simple, explainable relevance ranking
    /// (exact/prefix code matches score highest, followed by description word matches). The
    /// full reference list (~200-300 rows) is small enough to load into memory and rank in
    /// C# on every search, avoiding the complexity/cost of a real vector/embedding search while
    /// still feeling "smart" for free-text queries like "diabetes" or "chest pain".
    /// </summary>
    public class Icd10SearchService : IIcd10SearchService
    {
        private readonly ClinicalScribeDbContext _dbContext;

        /// <summary>Initializes a new instance of <see cref="Icd10SearchService"/>.</summary>
        /// <param name="dbContext">The database context used to load the ICD-10 code list.</param>
        public Icd10SearchService(ClinicalScribeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc />
        public async Task<List<Icd10Code>> SearchAsync(string query, int maxResults = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            var normalizedQuery = query.Trim().ToLowerInvariant();
            var queryWords = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // The full list is small (~200-300 rows), so it's simplest and fastest to load it
            // once and rank in memory rather than trying to express relevance scoring in SQL.
            var allCodes = await _dbContext.Icd10Codes.AsNoTracking().ToListAsync();

            var scored = allCodes
                .Select(code => (Code: code, Score: ScoreMatch(code, normalizedQuery, queryWords)))
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Code.Code)
                .Take(maxResults)
                .Select(x => x.Code)
                .ToList();

            return scored;
        }

        /// <summary>
        /// Scores a candidate code against the query: exact code match scores highest, then
        /// code-prefix match, then description matches (all words present scores higher than a
        /// partial substring match). Returns 0 if there's no match at all.
        /// </summary>
        private static int ScoreMatch(Icd10Code code, string normalizedQuery, string[] queryWords)
        {
            var normalizedCode = code.Code.ToLowerInvariant();
            var normalizedDescription = code.Description.ToLowerInvariant();

            if (normalizedCode == normalizedQuery)
            {
                return 100;
            }

            if (normalizedCode.StartsWith(normalizedQuery, StringComparison.Ordinal))
            {
                return 90;
            }

            if (normalizedDescription.Contains(normalizedQuery, StringComparison.Ordinal))
            {
                return 80;
            }

            // All query words appear somewhere in the description (any order) - handles
            // multi-word queries like "chest pain" matching "Chest pain, unspecified".
            if (queryWords.Length > 1 && queryWords.All(word => normalizedDescription.Contains(word, StringComparison.Ordinal)))
            {
                return 60;
            }

            // At least one query word matches - weakest relevance tier, still useful for
            // broad/partial queries.
            var matchingWordCount = queryWords.Count(word => normalizedDescription.Contains(word, StringComparison.Ordinal));
            if (matchingWordCount > 0)
            {
                return 30 + matchingWordCount;
            }

            return 0;
        }
    }
}
