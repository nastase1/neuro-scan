namespace NeuroScan.Application.IServices;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string firstName);
    Task SendPasswordResetCodeAsync(string toEmail, string firstName, string code);
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
