using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_AI.Models
{
    /// <summary>
    /// Represents a single embedded ICD-10 diagnosis code entry used to power the standalone
    /// ICD-10 search widget. A subset (200-300 entries) is seeded at startup; no external
    /// ICD-10 API is used at runtime.
    /// </summary>
    [Table("Icd10Codes")]
    public class Icd10Code
    {
        /// <summary>The unique identifier of the code entry.</summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>The ICD-10 code, e.g. "J45.909".</summary>
        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        /// <summary>The human-readable description of the diagnosis, e.g. "Unspecified asthma, uncomplicated".</summary>
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}
