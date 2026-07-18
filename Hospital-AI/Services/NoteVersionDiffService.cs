namespace Hospital_AI.Services
{
    /// <summary>The kind of change a single diff line represents.</summary>
    public enum DiffLineKind
    {
        /// <summary>The line is unchanged between the two versions.</summary>
        Unchanged,

        /// <summary>The line was added in the newer version (absent from the older version).</summary>
        Added,

        /// <summary>The line was removed in the newer version (present only in the older version).</summary>
        Removed
    }

    /// <summary>A single line in a computed diff, tagged with whether it was added, removed, or unchanged.</summary>
    /// <param name="Text">The line's text content.</param>
    /// <param name="Kind">Whether the line was added, removed, or unchanged.</param>
    public record DiffLine(string Text, DiffLineKind Kind);

    /// <summary>
    /// Computes a simple line-by-line diff between two blocks of text (e.g. the Subjective,
    /// Objective, Assessment, or Plan section of two saved <see cref="Hospital_AI.Models.NoteVersion"/>
    /// records), so a provider can see exactly what changed between versions rather than only
    /// the raw text of each version side by side.
    /// </summary>
    public interface INoteVersionDiffService
    {
        /// <summary>
        /// Computes a line-based diff between an older and a newer block of text, using a
        /// classic longest-common-subsequence approach so unchanged lines are matched even if
        /// lines were inserted/removed around them (not just a naive position-by-position
        /// comparison).
        /// </summary>
        /// <param name="oldText">The text of the older version's section.</param>
        /// <param name="newText">The text of the newer version's section.</param>
        List<DiffLine> ComputeDiff(string? oldText, string? newText);
    }

    /// <inheritdoc cref="INoteVersionDiffService" />
    public class NoteVersionDiffService : INoteVersionDiffService
    {
        /// <inheritdoc />
        public List<DiffLine> ComputeDiff(string? oldText, string? newText)
        {
            var oldLines = SplitLines(oldText);
            var newLines = SplitLines(newText);

            var lcsLengths = BuildLcsLengthTable(oldLines, newLines);

            var result = new List<DiffLine>();
            WalkBackAndBuildDiff(oldLines, newLines, lcsLengths, oldLines.Count, newLines.Count, result);

            return result;
        }

        private static List<string> SplitLines(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return [];
            }

            return text.Replace("\r\n", "\n").Split('\n').ToList();
        }

        // Standard dynamic-programming LCS length table: lengths[i][j] is the length of the
        // longest common subsequence of oldLines[0..i) and newLines[0..j).
        private static int[,] BuildLcsLengthTable(List<string> oldLines, List<string> newLines)
        {
            var lengths = new int[oldLines.Count + 1, newLines.Count + 1];

            for (var i = 1; i <= oldLines.Count; i++)
            {
                for (var j = 1; j <= newLines.Count; j++)
                {
                    lengths[i, j] = oldLines[i - 1] == newLines[j - 1]
                        ? lengths[i - 1, j - 1] + 1
                        : Math.Max(lengths[i - 1, j], lengths[i, j - 1]);
                }
            }

            return lengths;
        }

        // Walks the LCS table backwards from (i, j) to (0, 0), emitting Unchanged lines for LCS
        // matches, Removed lines for old-only lines, and Added lines for new-only lines, then
        // reverses the result since it's built in reverse order.
        private static void WalkBackAndBuildDiff(
            List<string> oldLines,
            List<string> newLines,
            int[,] lengths,
            int i,
            int j,
            List<DiffLine> result)
        {
            var reversed = new List<DiffLine>();

            while (i > 0 && j > 0)
            {
                if (oldLines[i - 1] == newLines[j - 1])
                {
                    reversed.Add(new DiffLine(oldLines[i - 1], DiffLineKind.Unchanged));
                    i--;
                    j--;
                }
                else if (lengths[i - 1, j] >= lengths[i, j - 1])
                {
                    reversed.Add(new DiffLine(oldLines[i - 1], DiffLineKind.Removed));
                    i--;
                }
                else
                {
                    reversed.Add(new DiffLine(newLines[j - 1], DiffLineKind.Added));
                    j--;
                }
            }

            while (i > 0)
            {
                reversed.Add(new DiffLine(oldLines[i - 1], DiffLineKind.Removed));
                i--;
            }

            while (j > 0)
            {
                reversed.Add(new DiffLine(newLines[j - 1], DiffLineKind.Added));
                j--;
            }

            reversed.Reverse();
            result.AddRange(reversed);
        }
    }
}
