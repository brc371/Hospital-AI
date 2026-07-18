namespace Hospital_AIChat.Models
{
    /// <summary>
    /// Data transfer object that mirrors the NoteResponse returned by the NoteKeeper REST API.
    /// </summary>
    public class NoteResponse
    {
        /// <summary>Gets or sets the unique identifier of the note.</summary>
        public Guid NoteId { get; set; }

        /// <summary>Gets or sets the summary text of the note.</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>Gets or sets the detailed content of the note.</summary>
        public string Details { get; set; } = string.Empty;

        /// <summary>Gets or sets the UTC timestamp when the note was created.</summary>
        public DateTimeOffset CreatedDateUtc { get; set; }

        /// <summary>Gets or sets the UTC timestamp when the note was last modified. Null if never modified.</summary>
        public DateTimeOffset? ModifiedDateUtc { get; set; }

        /// <summary>Gets or sets the list of tag names associated with the note.</summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
}
