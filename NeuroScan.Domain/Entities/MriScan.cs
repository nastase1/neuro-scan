namespace NeuroScan.Domain.Entities;

public class MriScan : BaseEntity
{
    public Guid PatientId { get; set; }
    public required string OriginalFileName { get; set; }
    public required string StoredFilePath { get; set; } // Path to .nii file (T1 for tumor scans)
    public string? StoredFilePathT1ce { get; set; }
    public string? StoredFilePathT2 { get; set; }
    public string? StoredFilePathFlair { get; set; }
    public ScanType ScanType { get; set; } = ScanType.Anatomy;
    public DateTime UploadDate { get; set; }
    public ScanStatus Status { get; set; }
    public Guid? ReviewedByDoctorId { get; set; }
    public string? DoctorClinicalNotes { get; set; }
    public string? CorrectedMaskPath { get; set; } // Path to doctor's correction
    public DateTime? ReviewedAt { get; set; }

    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public User? ReviewedByDoctor { get; set; }
    public AnalysisResult? AnalysisResult { get; set; }
}

public enum ScanType
{
    Anatomy = 0,
    TumorMultiChannel = 1
}

public enum ScanStatus
{
    Uploaded = 0,
    Processing = 1,
    Analyzed = 2,
    Failed = 3,
    ReviewedByDoctor = 4
}
