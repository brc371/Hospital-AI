using System.ComponentModel.DataAnnotations;

namespace Hospital_AI.Models
{
    /// <summary>
    /// The request payload for creating a new note.
    /// </summary>
    public class CreateNoteRequest
    {
        // [Required] checks for NULL and empty strings, but it does not check for whitespace-only strings.
        /// <summary>
        /// The summary of the note (1-60 characters, required, cannot be empty or whitespace only)
        /// </summary>
        /// <example>Azure tips and tricks</example>
        [Required(ErrorMessage = "Summary is required")]
        [StringLength(60, MinimumLength = 1, ErrorMessage = "Summary must be between 1 and 60 characters")]
        [RegularExpression(@"^(?!\s*$).+", ErrorMessage = "Summary cannot be empty or contain only whitespace")]
        public string Summary { get; set; } = string.Empty;

        // [Required] checks for NULL and empty strings, but it does not check for whitespace-only strings.
        /// <summary>
        /// The detailed content of the note (1-1024 characters, required, cannot be empty or whitespace only)
        /// </summary>
        /// <example>You can create custom dashboards to organize your resources</example>
        [Required(ErrorMessage = "Details are required")]
        [StringLength(1024, MinimumLength = 1, ErrorMessage = "Details must be between 1 and 1024 characters")]
        [RegularExpression(@"^(?!\s*$).+", ErrorMessage = "Details cannot be empty or contain only whitespace")]
        public string Details { get; set; } = string.Empty;
    }
}
