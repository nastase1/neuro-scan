namespace NeuroScan.Application.IServices;

public interface IAiAnalysisService
{
    Task<DualModelAnalysisResponseDTO> AnalyzeMriScanAsync(string niiFilePath);
}

public class DualModelAnalysisResponseDTO
{
    public bool Success { get; set; }
    public ModelResultDTO Model1 { get; set; } = null!;
    public ModelResultDTO Model2 { get; set; } = null!;
    public ComparisonDTO Comparison { get; set; } = null!;
}

public class ModelResultDTO
{
    public string Name { get; set; } = string.Empty;
    public double CsfVolume { get; set; }
    public double GmVolume { get; set; }
    public double WmVolume { get; set; }
    public double AsymmetryIndex { get; set; }
    public double ProcessingTime { get; set; }
}

public class ComparisonDTO
{
    public DiceScoresDTO DiceScores { get; set; } = null!;
    public double DisagreementPercentage { get; set; }
    public string RecommendedModel { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public VolumeDifferencesDTO VolumeDifferences { get; set; } = null!;
}

public class DiceScoresDTO
{
    public double Csf { get; set; }
    public double Gm { get; set; }
    public double Wm { get; set; }
    public double Average { get; set; }
}

public class VolumeDifferencesDTO
{
    public double Csf { get; set; }
    public double Gm { get; set; }
    public double Wm { get; set; }
}

// Legacy DTO for backward compatibility
public class AiAnalysisResponseDTO
{
    public double CsfVolume { get; set; }
    public double GmVolume { get; set; }
    public double WmVolume { get; set; }
    public double AsymmetryIndex { get; set; }
}
