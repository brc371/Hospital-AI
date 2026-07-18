using System.Text.RegularExpressions;

namespace Hospital_AI.Services
{
    /// <summary>
    /// Splits a generated or manually-written SOAP note's flat text into its four sections
    /// (Subjective, Objective, Assessment, Plan) by locating section header lines. Used when a
    /// provider saves a finalized <see cref="Models.NoteVersion"/> from the free-form draft
    /// note textarea.
    /// </summary>
    public static partial class SoapNoteParser
    {
        [GeneratedRegex(@"^\s*\**\s*(Subjective|Objective|Assessment|Plan)\s*\**\s*:\s*\**\s*", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex SectionHeaderRegex();

        /// <summary>
        /// Parses the given SOAP note text into its four sections. If a section header isn't
        /// found, that section is returned as an empty string. If none of the four headers are
        /// found at all, the entire text is placed into <c>Subjective</c> as a fallback so no
        /// content is silently lost.
        /// </summary>
        /// <param name="noteText">The flat SOAP note text to parse.</param>
        public static (string Subjective, string Objective, string Assessment, string Plan) Parse(string? noteText)
        {
            if (string.IsNullOrWhiteSpace(noteText))
            {
                return (string.Empty, string.Empty, string.Empty, string.Empty);
            }

            var matches = SectionHeaderRegex().Matches(noteText);

            if (matches.Count == 0)
            {
                // No recognizable headers - preserve the raw text rather than losing it.
                return (noteText.Trim(), string.Empty, string.Empty, string.Empty);
            }

            var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var sectionName = match.Groups[1].Value;
                var contentStart = match.Index + match.Length;
                var contentEnd = i + 1 < matches.Count ? matches[i + 1].Index : noteText.Length;
                var content = noteText[contentStart..contentEnd].Trim();

                sections[sectionName] = content;
            }

            return (
                sections.GetValueOrDefault("Subjective", string.Empty),
                sections.GetValueOrDefault("Objective", string.Empty),
                sections.GetValueOrDefault("Assessment", string.Empty),
                sections.GetValueOrDefault("Plan", string.Empty));
        }
    }
}
