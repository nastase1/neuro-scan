using Microsoft.AspNetCore.Http;

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
