namespace NeuroScan.Application.IServices;

public interface IAiAnalysisService
{
    Task<SegResNetAnalysisResponseDTO> AnalyzeMriScanAsync(string niiFilePath);
    Task<List<string>> GetRawSlicesAsync(string niiFilePath);
}

public class SegResNetAnalysisResponseDTO
{
    public bool Success { get; set; }
    public SegResNetResultDTO Segresnet { get; set; } = null!;
    public EpilepsyRiskDTO Epilepsy { get; set; } = null!;
    public List<string> SegmentationSlices { get; set; } = new();
}

public class SegResNetResultDTO
{
    public string Name { get; set; } = "SegResNet";
    public double CsfVolume { get; set; }
    public double GmVolume { get; set; }
    public double WmVolume { get; set; }
    public double AsymmetryIndex { get; set; }
    public double ProcessingTime { get; set; }
}

public class EpilepsyRiskDTO
{
    public double RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public List<string> Factors { get; set; } = new();
}

