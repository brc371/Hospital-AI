using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_AI.Models
{
    /// <summary>
    /// Represents a physician record in the Hospital database.
    /// </summary>
    [Table("Physicians")]
    public class Physician
    {
        /// <summary>The unique identifier of the physician.</summary>
        [Key]
        [Column("physician_id")]
        public int PhysicianId { get; set; }

        /// <summary>The physician's first name.</summary>
        [Column("first_name")]
        [StringLength(250)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>The physician's last name.</summary>
        [Column("last_name")]
        [StringLength(250)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>The physician's specialty.</summary>
        [Column("specialty")]
        [StringLength(250)]
        public string? Specialty { get; set; }

        /// <summary>The physician's department.</summary>
        [Column("department")]
        [StringLength(250)]
        public string? Department { get; set; }

        // Navigation properties
        /// <summary>Admissions where this physician is the attending physician.</summary>
        public ICollection<Admission> Admissions { get; set; } = [];

        /// <summary>Medications prescribed by this physician.</summary>
        public ICollection<Medication> Medications { get; set; } = [];
    }
}
