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
    // SegResNet volumetrics
    public double CsfVolume { get; set; }
    public double GmVolume { get; set; }
    public double WmVolume { get; set; }
    public double AsymmetryIndex { get; set; }
    // Epilepsy risk
    public double EpilepsyRiskScore { get; set; }
    public string EpilepsyRiskLevel { get; set; } = string.Empty;
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
