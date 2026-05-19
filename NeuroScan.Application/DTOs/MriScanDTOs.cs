using Microsoft.AspNetCore.Http;
using NeuroScan.Domain.Entities;

namespace NeuroScan.Application.IServices;

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
    public double CsfVolume { get; set; }
    public double GmVolume { get; set; }
    public double WmVolume { get; set; }
    public double AsymmetryIndex { get; set; }
    public double EpilepsyRiskScore { get; set; }
    public string EpilepsyRiskLevel { get; set; } = "Low";
    public bool TumorDetected { get; set; }
    public double TumorVolume { get; set; }
    public double TumorSurfaceArea { get; set; }
    public double CortexThicknessAvg { get; set; }
    public double CortexThicknessMin { get; set; }
    public double CortexThicknessMax { get; set; }
    public double WmDensityScore { get; set; }
    public double WmMeanIntensity { get; set; }
    public double WmCoefficientOfVariation { get; set; }
    public string? SegmentationImagePath { get; set; }
    public int SegmentationSliceCount { get; set; }
    public string? TumorOverlayImagePath { get; set; }
    public int TumorOverlaySliceCount { get; set; }
    public bool? DoctorApproved { get; set; }
    public string? DoctorReviewNotes { get; set; }
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
    public double BrainVolumeDelta { get; set; }
    public double BrainVolumeChangeRate { get; set; }
    public double GmVolumeDelta { get; set; }
    public double WmVolumeDelta { get; set; }
    public double CsfVolumeDelta { get; set; }
    public double TumorVolumeDelta { get; set; }
    public double CortexThicknessDelta { get; set; }
    public double WmDensityDelta { get; set; }
    public string DegradationLevel { get; set; } = "Stable";
    public int TotalScans { get; set; }
    public double MonthsSpan { get; set; }
}
