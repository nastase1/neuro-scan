namespace NeuroScan.Domain.Entities;

public class AnalysisResult : BaseEntity
{
    public Guid MriScanId { get; set; }

    // SegResNet Segmentation Results
    public double CsfVolume { get; set; }      // Cerebrospinal Fluid (cm³)
    public double GmVolume { get; set; }       // Gray Matter (cm³)
    public double WmVolume { get; set; }       // White Matter (cm³)
    public double AsymmetryIndex { get; set; } // Hemispheric asymmetry (%)

    // Epilepsy Risk Assessment
    public double EpilepsyRiskScore { get; set; }   // 0–100
    public string EpilepsyRiskLevel { get; set; } = string.Empty; // Low / Moderate / High

    // Tumor Detection (BraTS SegResNet)
    public bool TumorDetected { get; set; }
    public double TumorVolume { get; set; }         // cm³
    public double TumorSurfaceArea { get; set; }    // cm²

    // Cortex Thickness
    public double CortexThicknessAvg { get; set; }  // voxel units
    public double CortexThicknessMin { get; set; }
    public double CortexThicknessMax { get; set; }

    // White Matter Density
    public double WmDensityScore { get; set; }      // 0–100
    public double WmMeanIntensity { get; set; }
    public double WmCoefficientOfVariation { get; set; }

    // Segmentation Visualisation
    public string? SegmentationImagePath { get; set; } // Directory path for slice PNGs
    public int SegmentationSliceCount { get; set; }   // Number of saved slice images

    // Tumor Overlay Visualisation
    public string? TumorOverlayImagePath { get; set; }
    public int TumorOverlaySliceCount { get; set; }

    // Doctor Review
    public bool? DoctorApproved { get; set; }
    public string? DoctorReviewNotes { get; set; }

    // AI Medical Report
    public string? MedicalReportText { get; set; }
    public DateTime AnalyzedAt { get; set; }

    // Navigation property
    public MriScan MriScan { get; set; } = null!;
}

