using NeuroScan.Application.DTOs;

namespace NeuroScan.Application.IServices;

public interface IAiAnalysisService
{
    Task<SegResNetAnalysisResponseDTO> AnalyzeMriScanAsync(string niiFilePath);
    Task<SegResNetAnalysisResponseDTO> AnalyzeTumorScanAsync(string t1Path, string t1cePath, string t2Path, string flairPath);
    Task<List<string>> GetRawSlicesAsync(string niiFilePath);
    Task<MeshDataResponseDTO> Get3DMeshAsync(string niiFilePath);
    Task<MeshDataResponseDTO> Get3DMeshTumorAsync(string t1Path, string t1cePath, string t2Path, string flairPath);
}
