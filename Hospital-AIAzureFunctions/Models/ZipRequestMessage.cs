namespace Hospital_AIAzureFunctions.Models
{
    /// <summary>
    /// Represents the message payload dequeued from the attachment-zip-requests queue.
    /// Matches the message serialized by <c>AttachmentZipFilesController</c> in Hospital_AI.
    /// </summary>
    public class ZipRequestMessage
    {
        /// <summary>
        /// The id of the note whose attachments are to be compressed.
        /// </summary>
        public string NoteId { get; set; } = string.Empty;

        /// <summary>
        /// The blob name for the resulting zip archive (GUID + .zip suffix).
        /// Example: 4b7134af-6cc3-4aad-bb54-0da2e0f07928.zip
        /// </summary>
        public string ZipFileId { get; set; } = string.Empty;
    }
}
