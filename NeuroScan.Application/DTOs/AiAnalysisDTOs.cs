namespace NeuroScan.Application.DTOs;

public class SegResNetAnalysisResponseDTO
{
    public bool Success { get; set; }
    public SegResNetResultDTO Segresnet { get; set; } = null!;
    public EpilepsyRiskDTO Epilepsy { get; set; } = null!;
    public TumorResultDTO Tumor { get; set; } = new();
    public CortexThicknessDTO CortexThickness { get; set; } = new();
    public WhiteMatterDensityDTO WhiteMatterDensity { get; set; } = new();
    public List<string> SegmentationSlices { get; set; } = new();
    public List<string> TumorOverlaySlices { get; set; } = new();
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

public class TumorResultDTO
{
    public bool TumorDetected { get; set; }
    public double TumorVolume { get; set; }
    public double TumorSurfaceArea { get; set; }
    public int TumorVoxelCount { get; set; }
}

public class CortexThicknessDTO
{
    public double AvgThickness { get; set; }
    public double MinThickness { get; set; }
    public double MaxThickness { get; set; }
}

public class WhiteMatterDensityDTO
{
    public double MeanIntensity { get; set; }
    public double StdIntensity { get; set; }
    public double CoefficientOfVariation { get; set; }
    public double DensityScore { get; set; }
}

public class MeshDataResponseDTO
{
    public bool Success { get; set; }
    public Dictionary<string, MeshRegionDTO> Regions { get; set; } = new();
}

public class MeshRegionDTO
{
    public List<List<double>> Vertices { get; set; } = new();
    public List<List<int>> Faces { get; set; } = new();
    public string Color { get; set; } = "#888888";
    public double Opacity { get; set; } = 0.5;
}
