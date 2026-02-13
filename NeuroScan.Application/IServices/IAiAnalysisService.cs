namespace NeuroScan.Application.IServices;

public interface IAiAnalysisService
{
    Task<AiAnalysisResponseDTO> AnalyzeMriScanAsync(string niiFilePath);
}

public class AiAnalysisResponseDTO
{
    public double CsfVolume { get; set; }
    public double GmVolume { get; set; }
    public double WmVolume { get; set; }
    public double AsymmetryIndex { get; set; }
}
