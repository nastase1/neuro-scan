using Microsoft.AspNetCore.Http;
using NeuroScan.Domain.Entities;

namespace NeuroScan.Application.IServices;

public interface IMriScanService
{
    Task<MriScanResponseDTO> UploadAndProcessScanAsync(MriScanUploadDTO uploadDto, Guid userId);
    Task<MriScanResponseDTO> UploadSelfScanAsync(IFormFile file, string? notes, Guid userId);
    Task<MriScanResponseDTO> UploadAndProcessTumorScanAsync(MriScanUploadTumorDTO uploadDto, Guid userId);
    Task<MriScanResponseDTO> UploadSelfTumorScanAsync(IFormFile t1, IFormFile t1ce, IFormFile t2, IFormFile flair, string? notes, Guid userId);
    Task<MriScanDetailDTO?> GetScanDetailsAsync(Guid scanId, Guid userId, bool isDoctor = false);
    Task<IEnumerable<MriScanDetailDTO>> GetScansByPatientIdAsync(Guid patientId, Guid requesterId, bool isDoctor = false);
    Task<IEnumerable<MriScanDetailDTO>> GetMyScansAsync(Guid userId);
    Task SubmitCorrectedMaskAsync(Guid scanId, IFormFile correctedMask, Guid doctorId);
    Task<IEnumerable<MriScanSummaryDTO>> GetPendingReviewScansAsync();
    Task<int> GetRawSliceCountAsync(Guid scanId, Guid doctorId);
    Task<byte[]?> GetRawSliceAsync(Guid scanId, int sliceIndex, Guid doctorId);
    Task SubmitReviewAsync(Guid scanId, Guid doctorId, bool approved, string notes);
    Task SaveCorrectedSliceAsync(Guid scanId, int sliceIndex, string base64Png, Guid doctorId);
    Task<byte[]?> GetCorrectedSliceAsync(Guid scanId, int sliceIndex, Guid doctorId);
    Task<PatientEvolutionDTO> GetPatientEvolutionAsync(Guid patientId, Guid requesterId, bool isDoctor = false);
}

public class MriScanUploadDTO
{
    public Guid PatientId { get; set; }
    public required IFormFile File { get; set; }
}

public class MriScanUploadTumorDTO
{
    public Guid PatientId { get; set; }
    public required IFormFile T1File { get; set; }
    public required IFormFile T1ceFile { get; set; }
    public required IFormFile T2File { get; set; }
    public required IFormFile FlairFile { get; set; }
}

public class MriScanResponseDTO
{
    public Guid ScanId { get; set; }
    public string Message { get; set; } = string.Empty;
    public ScanStatus Status { get; set; }
}

public class MriScanDetailDTO
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFilePath { get; set; } = string.Empty;
    public string? StoredFilePathT1ce { get; set; }
    public string? StoredFilePathT2 { get; set; }
    public string? StoredFilePathFlair { get; set; }
    public ScanType ScanType { get; set; }
    public DateTime UploadDate { get; set; }
    public ScanStatus Status { get; set; }
    public string? DoctorClinicalNotes { get; set; }
    public PatientBasicDTO Patient { get; set; } = null!;
    public AnalysisResultDTO? AnalysisResult { get; set; }
}

public class PatientBasicDTO
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
}

public class AnalysisResultDTO
{
    // SegResNet volumetrics
    public double CsfVolume { get; set; }
    public double GmVolume { get; set; }
    public double WmVolume { get; set; }
    public double AsymmetryIndex { get; set; }

    // Epilepsy risk
    public double EpilepsyRiskScore { get; set; }
    public string EpilepsyRiskLevel { get; set; } = "Low";

    // Tumor detection
    public bool TumorDetected { get; set; }
    public double TumorVolume { get; set; }
    public double TumorSurfaceArea { get; set; }

    // Cortex thickness
    public double CortexThicknessAvg { get; set; }
    public double CortexThicknessMin { get; set; }
    public double CortexThicknessMax { get; set; }

    // White matter density
    public double WmDensityScore { get; set; }
    public double WmMeanIntensity { get; set; }
    public double WmCoefficientOfVariation { get; set; }

    // Segmentation image
    public string? SegmentationImagePath { get; set; }
    public int SegmentationSliceCount { get; set; }

    // Tumor overlay
    public string? TumorOverlayImagePath { get; set; }
    public int TumorOverlaySliceCount { get; set; }

    // Doctor Review
    public bool? DoctorApproved { get; set; }
    public string? DoctorReviewNotes { get; set; }

    // Report
    public string? MedicalReportText { get; set; }
    public DateTime AnalyzedAt { get; set; }
}

public class MriScanSummaryDTO
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public ScanStatus Status { get; set; }
}

public class PatientEvolutionDTO
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public List<EvolutionDataPointDTO> DataPoints { get; set; } = new();
    public EvolutionSummaryDTO Summary { get; set; } = new();
    public string? LlmInterpretation { get; set; }
}

public class EvolutionDataPointDTO
{
    public Guid ScanId { get; set; }
    public DateTime ScanDate { get; set; }
    public double CsfVolume { get; set; }
    public double GmVolume { get; set; }
    public double WmVolume { get; set; }
    public double TotalBrainVolume { get; set; }
    public double AsymmetryIndex { get; set; }
    public double EpilepsyRiskScore { get; set; }
    public bool TumorDetected { get; set; }
    public double TumorVolume { get; set; }
    public double TumorSurfaceArea { get; set; }
    public double CortexThicknessAvg { get; set; }
    public double WmDensityScore { get; set; }
}

public class EvolutionSummaryDTO
{
    public double BrainVolumeDelta { get; set; }       // Total change in cm³
    public double BrainVolumeChangeRate { get; set; }  // cm³ per month
    public double GmVolumeDelta { get; set; }
    public double WmVolumeDelta { get; set; }
    public double CsfVolumeDelta { get; set; }
    public double TumorVolumeDelta { get; set; }
    public double CortexThicknessDelta { get; set; }
    public double WmDensityDelta { get; set; }
    public string DegradationLevel { get; set; } = "Stable"; // Stable / Mild / Moderate / Severe
    public int TotalScans { get; set; }
    public double MonthsSpan { get; set; }
}
