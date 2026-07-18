using Hospital_AIChat.Models;
using Hospital_AIChat.Settings;
using System.Net.Http.Json;

namespace Hospital_AIChat
{
    /// <summary>
    /// HTTP client wrapper for the NoteKeeper REST API.
    /// All request URLs are constructed at call-time using <see cref="NoteKeeperSettings.GetActiveBaseUrl"/>
    /// so that switching the active endpoint at runtime is reflected immediately.
    /// </summary>
    public class NoteKeeperApiClient
    {
        /// <summary>The underlying HTTP client used to send requests to the NoteKeeper API.</summary>
        private readonly HttpClient _httpClient;

        /// <summary>The NoteKeeper API connection settings used to build request URLs.</summary>
        private readonly NoteKeeperSettings _settings;

        /// <summary>
        /// Initializes a new instance of <see cref="NoteKeeperApiClient"/>.
        /// </summary>
        /// <param name="httpClient">The underlying <see cref="HttpClient"/> instance.</param>
        /// <param name="settings">The NoteKeeper connection settings.</param>
        public NoteKeeperApiClient(HttpClient httpClient, NoteKeeperSettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
        }

        /// <summary>
        /// Builds a fully-qualified URL from the active base URL and the supplied relative path.
        /// </summary>
        /// <param name="relativePath">The API path relative to the base URL (e.g. "notes").</param>
        /// <returns>The fully-qualified URL string.</returns>
        private string BuildUrl(string relativePath) =>
            $"{_settings.GetActiveBaseUrl().TrimEnd('/')}/{relativePath.TrimStart('/')}";

        /// <summary>
        /// Retrieves all notes, optionally filtered by tag name.
        /// Corresponds to GET /notes or GET /notes?tagName={tagName}.
        /// </summary>
        /// <param name="tagName">Optional tag name filter.</param>
        /// <returns>A list of <see cref="NoteResponse"/> objects.</returns>
        public async Task<List<NoteResponse>> GetAllNotesAsync(string? tagName = null)
        {
            string url = BuildUrl("notes");
            if (!string.IsNullOrWhiteSpace(tagName))
                url += $"?tagName={Uri.EscapeDataString(tagName)}";

            var result = await _httpClient.GetFromJsonAsync<List<NoteResponse>>(url);
            return result ?? new List<NoteResponse>();
        }

        /// <summary>
        /// Retrieves a single note by its GUID.
        /// Corresponds to GET /notes/{noteId}.
        /// </summary>
        /// <param name="noteId">The GUID of the note to retrieve.</param>
        /// <returns>The <see cref="NoteResponse"/>, or <c>null</c> if not found.</returns>
        public async Task<NoteResponse?> GetNoteByIdAsync(string noteId)
        {
            string url = BuildUrl($"notes/{noteId}");
            var response = await _httpClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<NoteResponse>();
        }

        /// <summary>
        /// Retrieves all attachments for a given note.
        /// Corresponds to GET /notes/{noteId}/attachments.
        /// </summary>
        /// <param name="noteId">The GUID of the note.</param>
        /// <returns>A list of <see cref="AttachmentResponse"/> objects.</returns>
        public async Task<List<AttachmentResponse>> GetAttachmentsAsync(string noteId)
        {
            string url = BuildUrl($"notes/{noteId}/attachments");
            var response = await _httpClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new List<AttachmentResponse>();

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<List<AttachmentResponse>>();
            return result ?? new List<AttachmentResponse>();
        }

        /// <summary>
        /// Downloads the raw bytes of an attachment from Azure Blob Storage via the NoteKeeper API.
        /// Corresponds to GET /notes/{noteId}/attachments/{attachmentId}.
        /// </summary>
        /// <param name="noteId">The GUID of the note that owns the attachment.</param>
        /// <param name="attachmentId">The blob name (filename) of the attachment.</param>
        /// <returns>
        /// A tuple containing the raw file bytes and the MIME content-type returned by the API.
        /// </returns>
        public async Task<(byte[] Data, string ContentType)> DownloadAttachmentAsync(string noteId, string attachmentId)
        {
            string url = BuildUrl($"notes/{noteId}/attachments/{attachmentId}");
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            byte[] data = await response.Content.ReadAsByteArrayAsync();
            string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return (data, contentType);
        }
    }
}
