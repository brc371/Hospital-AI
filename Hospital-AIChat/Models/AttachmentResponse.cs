namespace Hospital_AIChat.Models
{
    /// <summary>
    /// Data transfer object that mirrors the AttachmentResponse returned by the NoteKeeper REST API.
    /// </summary>
    public class AttachmentResponse
    {
        /// <summary>Gets or sets the blob name (used as the attachment file name).</summary>
        public string AttachmentId { get; set; } = string.Empty;

        /// <summary>Gets or sets the MIME content type of the attachment.</summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>Gets or sets the date/time the blob was created as recorded by Azure Blob Storage.</summary>
        public DateTimeOffset CreatedDate { get; set; }

        /// <summary>Gets or sets the last modified date of the blob as recorded by Azure Blob Storage.</summary>
        public DateTimeOffset LastModifiedDate { get; set; }

        /// <summary>Gets or sets the size of the attachment in bytes.</summary>
        public long Length { get; set; }
    }
}
