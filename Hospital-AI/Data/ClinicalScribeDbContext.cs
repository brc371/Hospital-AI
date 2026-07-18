using Hospital_AI.Models;
using Microsoft.EntityFrameworkCore;

namespace Hospital_AI.Data
{
    /// <summary>
    /// Entity Framework Core <see cref="DbContext"/> for the AI Clinical Scribe platform.
    /// Contains DbSet properties for providers, patients, encounters, note versions,
    /// note templates, ICD-10 codes, and audit logs.
    /// </summary>
    public class ClinicalScribeDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ClinicalScribeDbContext"/> with the
        /// specified options.
        /// </summary>
        /// <param name="options">The options to configure the context (provider, connection string, etc.).</param>
        public ClinicalScribeDbContext(DbContextOptions<ClinicalScribeDbContext> options) : base(options)
        {
        }

        /// <summary>Gets or sets the providers/admins table.</summary>
        public DbSet<Provider> Providers { get; set; } = null!;

        /// <summary>Gets or sets the patients table.</summary>
        public DbSet<Patient> Patients { get; set; } = null!;

        /// <summary>Gets or sets the encounters table.</summary>
        public DbSet<Encounter> Encounters { get; set; } = null!;

        /// <summary>Gets or sets the note versions table (append-only audit trail of saved notes).</summary>
        public DbSet<NoteVersion> NoteVersions { get; set; } = null!;

        /// <summary>Gets or sets the note templates table.</summary>
        public DbSet<NoteTemplate> NoteTemplates { get; set; } = null!;

        /// <summary>Gets or sets the embedded ICD-10 code reference table.</summary>
        public DbSet<Icd10Code> Icd10Codes { get; set; } = null!;

        /// <summary>Gets or sets the audit log table.</summary>
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Providers: email is used to resolve the signed-in Entra External ID user to a
            // role, so it must be unique.
            modelBuilder.Entity<Provider>()
                .HasIndex(p => p.Email)
                .IsUnique();

            // Create index on FristName+LastName + DOB for patient matching. This allows us to detect returning patients across encounters.
            // Patients: matched by first name + last name + DOB to detect returning patients.
            modelBuilder.Entity<Patient>()
                .HasIndex(p => new { p.FirstName, p.LastName, p.DateOfBirth });

            // Encounter -> Patient (restrict delete: patients are never deleted via encounters)
            modelBuilder.Entity<Encounter>()
                .HasOne(e => e.Patient)
                .WithMany(p => p.Encounters)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Encounter -> Provider (restrict delete: providers are deactivated, not deleted)
            modelBuilder.Entity<Encounter>()
                .HasOne(e => e.Provider)
                .WithMany(p => p.Encounters)
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Encounter -> NoteTemplate (set null if a template is later deleted)
            modelBuilder.Entity<Encounter>()
                .HasOne(e => e.NoteTemplate)
                .WithMany(t => t.Encounters)
                .HasForeignKey(e => e.NoteTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            // NoteVersion -> Encounter (cascade: deleting an encounter removes its version history)
            modelBuilder.Entity<NoteVersion>()
                .HasOne(v => v.Encounter)
                .WithMany(e => e.NoteVersions)
                .HasForeignKey(v => v.EncounterId)
                .OnDelete(DeleteBehavior.Cascade);

            // NoteVersion -> Provider (restrict: preserve audit trail even if provider is deactivated)
            modelBuilder.Entity<NoteVersion>()
                .HasOne(v => v.SavedByProvider)
                .WithMany(p => p.SavedNoteVersions)
                .HasForeignKey(v => v.SavedByProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ensure version numbers are unique per encounter.
            modelBuilder.Entity<NoteVersion>()
                .HasIndex(v => new { v.EncounterId, v.VersionNumber })
                .IsUnique();

            // AuditLog -> Provider (restrict: preserve audit trail even if provider is deactivated)
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.PerformedByProvider)
                .WithMany()
                .HasForeignKey(a => a.PerformedByProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // ICD-10 codes: enforce unique code values.
            modelBuilder.Entity<Icd10Code>()
                .HasIndex(i => i.Code)
                .IsUnique();

            // Store enums as strings for readability in the database.
            modelBuilder.Entity<Provider>()
                .Property(p => p.Role)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Encounter>()
                .Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
        }
    }
}
