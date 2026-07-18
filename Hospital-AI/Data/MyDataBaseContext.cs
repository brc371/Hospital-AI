using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Humanizer;
using Hospital_AI.Models;

namespace Hospital_AI.Data
{
    /// <summary>
    /// Entity Framework Core <see cref="DbContext"/> for the Note Keeper application.
    /// Contains DbSet properties for notes and tags and EF model configuration.
    /// </summary>
    public class MyDataBaseContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MyDataBaseContext"/> with the specified options.
        /// </summary>
        /// <param name="options">The options to configure the context (provider, connection string, etc.).</param>
        public MyDataBaseContext(DbContextOptions<MyDataBaseContext> options) : base(options)
        {
        }
              
        /// <summary>
        /// Gets or sets the patients table.
        /// </summary>
        public DbSet<Patient> Patients { get; set; } = null!;

        /// <summary>
        /// Gets or sets the lab results table.
        /// </summary>
        public DbSet<LabResult> LabResults { get; set; } = null!;

        /// <summary>
        /// Gets or sets the physicians table.
        /// </summary>
        public DbSet<Physician> Physicians { get; set; } = null!;

        /// <summary>
        /// Gets or sets the admissions table.
        /// </summary>
        public DbSet<Admission> Admissions { get; set; } = null!;

        /// <summary>
        /// Gets or sets the medications table.
        /// </summary>
        public DbSet<Medication> Medications { get; set; } = null!;

        /// <summary>
        /// Gets or sets the medical history table.
        /// </summary>
        public DbSet<MedicalHistory> MedicalHistories { get; set; } = null!;

        /// <summary>
        /// Configure the EF Core model. This method singularizes table names and configures the
        /// relationship between <see cref="Tag"/> and <see cref="Note"/>.
        /// </summary>
        /// <param name="modelBuilder">The model builder used to configure entity mappings.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            // Configure Admissions relationships
            modelBuilder.Entity<Admission>(eb =>
            {
                eb.HasOne(a => a.Patient)
                  .WithMany(p => p.Admissions)
                  .HasForeignKey(a => a.PatientId)
                  .HasConstraintName("FK_Admissions_Patients");

                eb.HasOne(a => a.Physician)
                  .WithMany(p => p.Admissions)
                  .HasForeignKey(a => a.PhysicianId)
                  .HasConstraintName("FK_Admissions_Physicians");
            });

            // Configure LabResults relationships
            modelBuilder.Entity<LabResult>(eb =>
            {
                eb.HasOne(l => l.Patient)
                  .WithMany(p => p.LabResults)
                  .HasForeignKey(l => l.PatientId)
                  .HasConstraintName("FK_LabResults_Patients");

                eb.HasOne(l => l.Admission)
                  .WithMany(a => a.LabResults)
                  .HasForeignKey(l => l.AdmissionId)
                  .HasConstraintName("FK_LabResults_Admissions");
            });

            // Configure MedicalHistory relationship
            modelBuilder.Entity<MedicalHistory>(eb =>
            {
                eb.HasOne(m => m.Patient)
                  .WithOne(p => p.MedicalHistory)
                  .HasForeignKey<MedicalHistory>(m => m.PatientId)
                  .HasConstraintName("FK_MedicalHistory_Patients");
            });

            // Configure Medications relationships
            modelBuilder.Entity<Medication>(eb =>
            {
                eb.HasOne(m => m.Patient)
                  .WithMany(p => p.Medications)
                  .HasForeignKey(m => m.PatientId)
                  .HasConstraintName("FK_Medications_Patients");

                eb.HasOne(m => m.Physician)
                  .WithMany(p => p.Medications)
                  .HasForeignKey(m => m.PrescribingPhysicianId)
                  .HasConstraintName("FK_Medications_Physicians");
            });
        }
    }
}