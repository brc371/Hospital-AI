using Hospital_AI.Models;

namespace Hospital_AI.Services
{
    /// <summary>
    /// Manages admin-authored <see cref="NoteTemplate"/> records: structured prompts that shape
    /// how the AI generates SOAP notes for different encounter types. Providers select a
    /// template before generating a note; admins can create, edit, and delete templates, and
    /// changes take effect immediately (providers re-read the latest template on every
    /// generation, so no caching/refresh is required).
    /// </summary>
    public interface ITemplateService
    {
        /// <summary>Retrieves all templates, most recently updated first.</summary>
        Task<List<NoteTemplate>> GetAllAsync();

        /// <summary>Retrieves only active templates, ordered by name. Used to populate the provider's template picker.</summary>
        Task<List<NoteTemplate>> GetActiveAsync();

        /// <summary>Retrieves a single template by ID, or <c>null</c> if not found.</summary>
        Task<NoteTemplate?> GetByIdAsync(Guid id);

        /// <summary>Creates a new template.</summary>
        /// <param name="name">The display name of the template.</param>
        /// <param name="promptText">The prompt instructions injected into the AI system message.</param>
        Task<NoteTemplate> CreateAsync(string name, string promptText);

        /// <summary>
        /// Updates an existing template's name, prompt text, and active flag, stamping
        /// <see cref="NoteTemplate.UpdatedAtUtc"/> so in-progress generations pick up the change
        /// immediately. Returns <c>null</c> if no template with the given ID exists.
        /// </summary>
        Task<NoteTemplate?> UpdateAsync(Guid id, string name, string promptText, bool isActive);

        /// <summary>Deletes a template. Encounters that referenced it retain their history via a nulled foreign key.</summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
