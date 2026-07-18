using Hospital_AIChat.Models;
using System.ComponentModel;
using System.Text;

namespace Hospital_AIChat
{
    // ─────────────────────────────────────────────────────────────────────────
    // Supporting model classes returned by the AI tools.
    // These are separate from the API response DTOs so the AI receives a
    // clean, numbered representation that supports natural-language references
    // such as "give me the details for note 2".
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Represents one entry in a numbered notes list returned by the AI tool.
    /// </summary>
    public class NoteListItem
    {
        /// <summary>Gets or sets the 1-based position number shown to the user.</summary>
        public int Number { get; set; }

        /// <summary>Gets or sets the internal GUID of the note.</summary>
        public string NoteId { get; set; } = string.Empty;

        /// <summary>Gets or sets the summary of the note.</summary>
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents an attachment entry in a numbered list returned by the AI tool.
    /// </summary>
    public class AttachmentListItem
    {
        /// <summary>Gets or sets the 1-based position number shown to the user.</summary>
        public int Number { get; set; }

        /// <summary>Gets or sets the attachment ID, which is also the blob/file name.</summary>
        public string AttachmentId { get; set; } = string.Empty;

        /// <summary>Gets or sets the MIME content type.</summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>Gets or sets the file size in bytes.</summary>
        public long Length { get; set; }
    }

    /// <summary>
    /// Represents the full details for a single note, including its attachments.
    /// </summary>
    public class NoteDetail
    {
        /// <summary>Gets or sets the internal GUID of the note.</summary>
        public string NoteId { get; set; } = string.Empty;

        /// <summary>Gets or sets the summary of the note.</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>Gets or sets the detailed content of the note.</summary>
        public string Details { get; set; } = string.Empty;

        /// <summary>Gets or sets the UTC creation timestamp.</summary>
        public DateTimeOffset CreatedDateUtc { get; set; }

        /// <summary>Gets or sets the UTC last-modified timestamp. Null if never modified.</summary>
        public DateTimeOffset? ModifiedDateUtc { get; set; }

        /// <summary>Gets or sets the list of AI-generated tag names associated with the note.</summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>Gets or sets the numbered list of attachments for the note.</summary>
        public List<AttachmentListItem> Attachments { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AI Tool definitions
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Provides AI tool functions for the NoteKeeper Chat application.
    /// Each public method decorated with <see cref="DescriptionAttribute"/> is
    /// registered as an AI function that the model may invoke automatically.
    /// </summary>
    public class NoteKeeperTools
    {
        private readonly NoteKeeperApiClient _apiClient;

        /// <summary>
        /// Initializes a new instance of <see cref="NoteKeeperTools"/>.
        /// </summary>
        /// <param name="apiClient">The NoteKeeper REST API client.</param>
        public NoteKeeperTools(NoteKeeperApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// Retrieves all notes from the NoteKeeper API, optionally filtered by a tag name.
        /// Returns a numbered list so the user can reference notes by position number in follow-up
        /// messages (e.g. "give me the details for note 2").
        /// </summary>
        /// <param name="tagName">
        /// Optional tag name to filter notes. Pass null or an empty string to retrieve all notes.
        /// </param>
        /// <returns>A numbered list of <see cref="NoteListItem"/> objects.</returns>
        [Description("Retrieve all notes from the NoteKeeper API. Returns a numbered list where each item has a Number (1-based position), NoteId (GUID), and Summary. Optionally filter by tagName. Always show the list with numbers so the user can reference them by number (e.g. 'note 2') in follow-up questions.")]
        public async Task<List<NoteListItem>> GetAllNotesAsync(
            [Description("Optional tag name to filter notes by. Leave null or empty to return all notes.")] string? tagName = null)
        {
            ConsoleHelpers.ConsoleWriteLine(ConsoleColor.DarkBlue,
                $"\nAI Tool: GetAllNotes{(tagName != null ? $"(tag=\"{tagName}\")" : "()")}");

            var notes = await _apiClient.GetAllNotesAsync(tagName);

            return notes.Select((n, i) => new NoteListItem
            {
                Number = i + 1,
                NoteId = n.NoteId.ToString(),
                Summary = n.Summary
            }).ToList();
        }

        /// <summary>
        /// Retrieves full details for a single note by its NoteId GUID, including its tags and
        /// a numbered list of attachments.
        /// </summary>
        /// <param name="noteId">The GUID of the note to retrieve.</param>
        /// <returns>
        /// A <see cref="NoteDetail"/> with all note fields and attachments, or <c>null</c> if
        /// the note was not found.
        /// </returns>
        [Description("Retrieve the full details of a single note by its NoteId (GUID). Returns the note's Summary, Details, Tags, CreatedDateUtc, ModifiedDateUtc, and a numbered list of Attachments (each with Number, AttachmentId, ContentType, Length). Returns null if the note does not exist.")]
        public async Task<NoteDetail?> GetNoteByIdAsync(
            [Description("The GUID of the note to retrieve. This is the NoteId value from the notes list.")] string noteId)
        {
            ConsoleHelpers.ConsoleWriteLine(ConsoleColor.DarkBlue, $"\nAI Tool: GetNoteById(\"{noteId}\")");

            NoteResponse? note;
            try
            {
                note = await _apiClient.GetNoteByIdAsync(noteId);
            }
            catch (HttpRequestException)
            {
                return null;
            }

            if (note == null)
                return null;

            List<AttachmentResponse> attachments = new();
            try
            {
                attachments = await _apiClient.GetAttachmentsAsync(noteId);
            }
            catch (HttpRequestException)
            {
                // Return note details even if attachments cannot be fetched.
            }

            return new NoteDetail
            {
                NoteId = note.NoteId.ToString(),
                Summary = note.Summary,
                Details = note.Details,
                CreatedDateUtc = note.CreatedDateUtc,
                ModifiedDateUtc = note.ModifiedDateUtc,
                Tags = note.Tags,
                Attachments = attachments.Select((a, i) => new AttachmentListItem
                {
                    Number = i + 1,
                    AttachmentId = a.AttachmentId,
                    ContentType = a.ContentType,
                    Length = a.Length
                }).ToList()
            };
        }

        /// <summary>
        /// Downloads an attachment for the specified note and saves it to the local filesystem.
        /// </summary>
        /// <param name="noteId">The GUID of the note that owns the attachment.</param>
        /// <param name="attachmentId">The attachment ID (blob name / filename) to download.</param>
        /// <param name="savePath">
        /// Optional directory path on the local filesystem where the file should be saved.
        /// Defaults to the user's Downloads folder if not provided.
        /// </param>
        /// <returns>The full path to the saved file.</returns>
        [Description("Download a specific attachment for a note and save it to the local filesystem. Returns the full path to the saved file. Use the AttachmentId from the note's attachment list as the attachmentId parameter.")]
        public async Task<string> DownloadAttachmentAsync(
            [Description("The GUID of the note that owns the attachment.")] string noteId,
            [Description("The attachment ID (blob name / filename) to download, e.g. 'MilkAndEggs.png'.")] string attachmentId,
            [Description("Optional local directory path where the file will be saved. Defaults to the user's Downloads folder.")] string? savePath = null)
        {
            ConsoleHelpers.ConsoleWriteLine(ConsoleColor.DarkBlue,
                $"\nAI Tool: DownloadAttachment(noteId=\"{noteId}\", attachmentId=\"{attachmentId}\")");

            var (data, _) = await _apiClient.DownloadAttachmentAsync(noteId, attachmentId);

            string targetDir = string.IsNullOrWhiteSpace(savePath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
                : savePath;

            Directory.CreateDirectory(targetDir);

            string fullPath = Path.Combine(targetDir, attachmentId);
            await File.WriteAllBytesAsync(fullPath, data);

            ConsoleHelpers.ConsoleWriteLine(ConsoleColor.DarkGreen, $"\nAI Tool: Saved attachment to {fullPath}");
            return fullPath;
        }

        /// <summary>
        /// Writes text content to a file on the local filesystem.
        /// Use this tool when the user asks to create a file with specific content,
        /// such as a markdown file or a plain text file.
        /// </summary>
        /// <param name="filePath">
        /// The filename or full path for the file to create.
        /// If only a filename is provided, the file is saved in the current working directory.
        /// </param>
        /// <param name="content">The text content to write to the file.</param>
        /// <returns>The complete path where the file was written.</returns>
        [Description("Write text content to a file on the local filesystem. Use this when the user asks to create or save a file (e.g. a markdown file, a text list, etc.). If filePath is just a filename, the file is saved in the current working directory. Returns the full path to the written file.")]
        public async Task<string> WriteDataToFileAsync(
            [Description("The filename or full path for the output file, e.g. 'shoppinglist.md' or 'C:\\Users\\brian\\notes.txt'.")] string filePath,
            [Description("The full text content to write to the file.")] string content)
        {
            ConsoleHelpers.ConsoleWriteLine(ConsoleColor.DarkBlue, $"\nAI Tool: WriteDataToFile(\"{filePath}\")");

            string fullPath = Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(Directory.GetCurrentDirectory(), filePath);

            string? dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(fullPath, content, Encoding.UTF8);

            ConsoleHelpers.ConsoleWriteLine(ConsoleColor.DarkGreen, $"\nAI Tool: File written to {fullPath}");
            return fullPath;
        }
    }
}
