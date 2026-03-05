using Microsoft.EntityFrameworkCore;
using NeuroScan.Domain.Entities;

namespace NeuroScan.Infrastructure.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<MriScan> MriScans { get; set; }
    public DbSet<AnalysisResult> AnalysisResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.DeletedAt);
            entity.HasIndex(e => e.InviteCode).IsUnique().HasFilter("[InviteCode] IS NOT NULL");

            // Self-referencing: StandardUser → AssignedDoctor
            entity.HasOne(e => e.AssignedDoctor)
                .WithMany()
                .HasForeignKey(e => e.AssignedDoctorId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Patient configuration
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MedicalRecordNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.MedicalRecordNumber).IsUnique();

            entity.HasOne(e => e.CreatedBy)
                .WithMany(u => u.Patients)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MriScan configuration
        modelBuilder.Entity<MriScan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StoredFilePath).IsRequired().HasMaxLength(500);

            entity.HasOne(e => e.Patient)
                .WithMany(p => p.MriScans)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ReviewedByDoctor)
                .WithMany(u => u.ReviewedScans)
                .HasForeignKey(e => e.ReviewedByDoctorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // AnalysisResult configuration (1-to-1 with MriScan)
        modelBuilder.Entity<AnalysisResult>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.MriScan)
                .WithOne(m => m.AnalysisResult)
                .HasForeignKey<AnalysisResult>(e => e.MriScanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
