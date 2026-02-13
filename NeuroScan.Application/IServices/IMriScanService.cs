using Microsoft.AspNetCore.Http;
using NeuroScan.Domain.Entities;

namespace NeuroScan.Application.IServices;

public interface IMriScanService
{
    Task<MriScanResponseDTO> UploadAndProcessScanAsync(MriScanUploadDTO uploadDto, Guid userId);
    Task<MriScanDetailDTO?> GetScanDetailsAsync(Guid scanId, Guid userId);
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
    public double CsfVolume { get; set; }
    public double GmVolume { get; set; }
    public double WmVolume { get; set; }
    public double AsymmetryIndex { get; set; }
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
