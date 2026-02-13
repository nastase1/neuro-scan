namespace NeuroScan.Domain.Entities;

public class MriScan : BaseEntity
{
    public Guid PatientId { get; set; }
    public required string OriginalFileName { get; set; }
    public required string StoredFilePath { get; set; } // Path to .nii file
    public DateTime UploadDate { get; set; }
    public ScanStatus Status { get; set; }
    public Guid? ReviewedByDoctorId { get; set; }
    public string? CorrectedMaskPath { get; set; } // Path to doctor's correction
    public DateTime? ReviewedAt { get; set; }

    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public User? ReviewedByDoctor { get; set; }
    public AnalysisResult? AnalysisResult { get; set; }
}

public enum ScanStatus
{
    Uploaded = 0,
    Processing = 1,
    Analyzed = 2,
    Failed = 3,
    ReviewedByDoctor = 4
}
