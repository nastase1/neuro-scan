namespace NeuroScan.Application.IServices;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string firstName);
    Task SendPasswordResetCodeAsync(string toEmail, string firstName, string code);
    Task SendScanResultsEmailAsync(string toEmail, string patientName, ScanResultEmailData data);
}

public class ScanResultEmailData
{
    public DateTime ScanDate { get; set; }
    public string MedicalReport { get; set; } = string.Empty;
    // Model 1 (UNet)
    public double CsfVolume { get; set; }
    public double GmVolume { get; set; }
    public double WmVolume { get; set; }
    public double AsymmetryIndex { get; set; }
    // Model 2 (SegResNet)
    public double CsfVolumeModel2 { get; set; }
    public double GmVolumeModel2 { get; set; }
    public double WmVolumeModel2 { get; set; }
    public double AsymmetryIndexModel2 { get; set; }
    // Comparison
    public double DiceScoreCsf { get; set; }
    public double DiceScoreGm { get; set; }
    public double DiceScoreWm { get; set; }
    public double DisagreementPercentage { get; set; }
    public string RecommendedModel { get; set; } = string.Empty;
    public double ModelConfidence { get; set; }
}

public class EmailSettings
{
    public required string SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public required string SmtpUser { get; set; }
    public required string SmtpPassword { get; set; }
    public required string FromEmail { get; set; }
    public string FromName { get; set; } = "NeuroScan";
}
