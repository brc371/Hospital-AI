using Hospital_AI.Data;
using Hospital_AI.Models;
using Microsoft.EntityFrameworkCore;

namespace Hospital_AI.Services
{
    /// <inheritdoc cref="ITemplateService" />
    public class TemplateService : ITemplateService
    {
        private readonly ClinicalScribeDbContext _dbContext;

        /// <summary>Initializes a new instance of <see cref="TemplateService"/>.</summary>
        public TemplateService(ClinicalScribeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc />
        public async Task<List<NoteTemplate>> GetAllAsync()
        {
            return await _dbContext.NoteTemplates
                .OrderByDescending(t => t.UpdatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<NoteTemplate>> GetActiveAsync()
        {
            return await _dbContext.NoteTemplates
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<NoteTemplate?> GetByIdAsync(Guid id)
        {
            return await _dbContext.NoteTemplates.FirstOrDefaultAsync(t => t.Id == id);
        }

        /// <inheritdoc />
        public async Task<NoteTemplate> CreateAsync(string name, string promptText)
        {
            var template = new NoteTemplate
            {
                Id = Guid.NewGuid(),
                Name = name,
                PromptText = promptText,
                IsActive = true
            };

            _dbContext.NoteTemplates.Add(template);
            await _dbContext.SaveChangesAsync();

            return template;
        }

        /// <inheritdoc />
        public async Task<NoteTemplate?> UpdateAsync(Guid id, string name, string promptText, bool isActive)
        {
            var template = await _dbContext.NoteTemplates.FirstOrDefaultAsync(t => t.Id == id);
            if (template is null)
            {
                return null;
            }

            template.Name = name;
            template.PromptText = promptText;
            template.IsActive = isActive;
            template.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync();

            return template;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(Guid id)
        {
            var template = await _dbContext.NoteTemplates.FirstOrDefaultAsync(t => t.Id == id);
            if (template is null)
            {
                return false;
            }

            _dbContext.NoteTemplates.Remove(template);
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}
