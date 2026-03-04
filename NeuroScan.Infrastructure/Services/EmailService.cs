using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NeuroScan.Application.IServices;

namespace NeuroScan.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string firstName)
    {
        var subject = "Welcome to NeuroScan – Account Created Successfully";
        var body = $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: Arial, sans-serif; background:#0a0a12; color:#e2e8f0; padding:32px;">
              <div style="max-width:560px; margin:0 auto; background:#12121e; border-radius:16px; padding:40px; border:1px solid rgba(124,58,237,0.2);">
                <div style="text-align:center; margin-bottom:32px;">
                  <div style="display:inline-block; background:linear-gradient(135deg,#7c3aed,#6366f1); border-radius:12px; padding:16px; margin-bottom:16px;">
                    <span style="font-size:32px;">🧠</span>
                  </div>
                  <h1 style="color:#fff; font-size:24px; margin:0;">Welcome to NeuroScan</h1>
                </div>
                <p style="color:#94a3b8; line-height:1.7;">Hi <strong style="color:#c4b5fd;">{firstName}</strong>,</p>
                <p style="color:#94a3b8; line-height:1.7;">
                  Your NeuroScan account has been <strong style="color:#a78bfa;">successfully created</strong>. 
                  You can now sign in and start using our AI-powered brain MRI analysis platform.
                </p>
                <div style="background:rgba(124,58,237,0.1); border:1px solid rgba(124,58,237,0.2); border-radius:12px; padding:20px; margin:24px 0;">
                  <p style="color:#c4b5fd; margin:0; font-size:14px;">🔒 Keep your credentials safe. If you did not create this account, please contact support immediately.</p>
                </div>
                <div style="text-align:center; margin-top:32px;">
                  <a href="http://localhost:4200/login" style="background:linear-gradient(135deg,#7c3aed,#6366f1); color:#fff; text-decoration:none; padding:12px 32px; border-radius:10px; font-weight:bold; display:inline-block;">
                    Sign In to NeuroScan
                  </a>
                </div>
                <p style="color:#475569; font-size:12px; text-align:center; margin-top:32px;">
                  © 2026 NeuroScan. Medical-grade AI imaging platform.
                </p>
              </div>
            </body>
            </html>
            """;

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetCodeAsync(string toEmail, string firstName, string code)
    {
        var subject = "NeuroScan – Your Password Reset Code";
        var body = $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: Arial, sans-serif; background:#0a0a12; color:#e2e8f0; padding:32px;">
              <div style="max-width:560px; margin:0 auto; background:#12121e; border-radius:16px; padding:40px; border:1px solid rgba(124,58,237,0.2);">
                <div style="text-align:center; margin-bottom:32px;">
                  <div style="display:inline-block; background:linear-gradient(135deg,#7c3aed,#6366f1); border-radius:12px; padding:16px; margin-bottom:16px;">
                    <span style="font-size:32px;">🔐</span>
                  </div>
                  <h1 style="color:#fff; font-size:24px; margin:0;">Password Reset Request</h1>
                </div>
                <p style="color:#94a3b8; line-height:1.7;">Hi <strong style="color:#c4b5fd;">{firstName}</strong>,</p>
                <p style="color:#94a3b8; line-height:1.7;">
                  We received a request to reset your password. Use the 6-digit code below:
                </p>
                <div style="text-align:center; margin:32px 0;">
                  <div style="display:inline-block; background:rgba(124,58,237,0.15); border:2px solid rgba(124,58,237,0.4); border-radius:16px; padding:24px 48px;">
                    <span style="font-size:42px; font-weight:bold; color:#a78bfa; letter-spacing:12px;">{code}</span>
                  </div>
                </div>
                <div style="background:rgba(251,191,36,0.08); border:1px solid rgba(251,191,36,0.2); border-radius:12px; padding:20px; margin:24px 0;">
                  <p style="color:#fbbf24; margin:0; font-size:14px;">⏱️ This code is valid for <strong>15 minutes</strong>. If you did not request a password reset, you can safely ignore this email.</p>
                </div>
                <p style="color:#475569; font-size:12px; text-align:center; margin-top:32px;">
                  © 2026 NeuroScan. Medical-grade AI imaging platform.
                </p>
              </div>
            </body>
            </html>
            """;

        await SendEmailAsync(toEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(new MailboxAddress(string.Empty, toEmail));
        message.Subject = subject;

        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
