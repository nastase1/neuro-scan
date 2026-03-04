using Microsoft.AspNetCore.Http;
using NeuroScan.Domain.Entities;

namespace NeuroScan.Application.IServices;

public interface IMriScanService
{
    Task<MriScanResponseDTO> UploadAndProcessScanAsync(MriScanUploadDTO uploadDto, Guid userId);
    Task<MriScanResponseDTO> UploadSelfScanAsync(IFormFile file, string? notes, Guid userId);
    Task<MriScanDetailDTO?> GetScanDetailsAsync(Guid scanId, Guid userId, bool isDoctor = false);
    Task<IEnumerable<MriScanDetailDTO>> GetScansByPatientIdAsync(Guid patientId, Guid doctorId);
    Task<IEnumerable<MriScanDetailDTO>> GetMyScansAsync(Guid userId);
    Task SubmitCorrectedMaskAsync(Guid scanId, IFormFile correctedMask, Guid doctorId);
    Task<IEnumerable<MriScanSummaryDTO>> GetPendingReviewScansAsync();
}

public class MriScanUploadDTO
{
    public Guid PatientId { get; set; }
    public required IFormFile File { get; set; }
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
    public DateTime UploadDate { get; set; }
    public ScanStatus Status { get; set; }
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
    // Model 1 (UNet) results
    public double CsfVolume { get; set; }
    public double GmVolume { get; set; }
    public double WmVolume { get; set; }
    public double AsymmetryIndex { get; set; }

    // Model 2 (SegResNet) results
    public double CsfVolumeModel2 { get; set; }
    public double GmVolumeModel2 { get; set; }
    public double WmVolumeModel2 { get; set; }
    public double AsymmetryIndexModel2 { get; set; }

    // Comparison metrics
    public double DiceScoreCsf { get; set; }
    public double DiceScoreGm { get; set; }
    public double DiceScoreWm { get; set; }
    public double DisagreementPercentage { get; set; }
    public string RecommendedModel { get; set; } = "unet";
    public double ModelConfidence { get; set; }

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
