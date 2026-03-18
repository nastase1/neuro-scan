using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NeuroScan.Application.IServices;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NeuroScan.Infrastructure.Services;

public class EmailService : IEmailService
{
  private readonly EmailSettings _settings;

  public EmailService(IOptions<EmailSettings> settings)
  {
    _settings = settings.Value;
    QuestPDF.Settings.License = LicenseType.Community;
  }

  public async Task SendWelcomeEmailAsync(string toEmail, string firstName)
  {
    var subject = "Welcome to NeuroScan – Account Created Successfully";
    var body = $"""
            <!DOCTYPE html>
            <html>
            <body style="margin:0; padding:0; background:#f3f4f6; font-family: Arial, Helvetica, sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6; padding:40px 0;">
                <tr><td align="center">
                  <table width="560" cellpadding="0" cellspacing="0" style="background:#ffffff; border-radius:12px; overflow:hidden; border:1px solid #e5e7eb;">
                    <!-- Header -->
                    <tr>
                      <td style="background:linear-gradient(135deg,#7c3aed,#6366f1); padding:32px 40px; text-align:center;">
                        <div style="font-size:36px; margin-bottom:12px;">🧠</div>
                        <h1 style="margin:0; color:#ffffff; font-size:22px; font-weight:700; letter-spacing:-0.3px;">Welcome to NeuroScan</h1>
                      </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                      <td style="padding:36px 40px;">
                        <p style="margin:0 0 16px; color:#111827; font-size:15px; line-height:1.7;">Hi <strong>{firstName}</strong>,</p>
                        <p style="margin:0 0 16px; color:#374151; font-size:15px; line-height:1.7;">
                          Your NeuroScan account has been <strong>successfully created</strong>.
                          You can now sign in and start using our AI-powered brain MRI analysis platform.
                        </p>
                        <div style="background:#f5f3ff; border:1px solid #ddd6fe; border-radius:8px; padding:16px 20px; margin:24px 0;">
                          <p style="margin:0; color:#5b21b6; font-size:14px;">🔒 Keep your credentials safe. If you did not create this account, please contact support immediately.</p>
                        </div>
                        <div style="text-align:center; margin-top:32px;">
                          <a href="http://localhost:4200/login" style="background:linear-gradient(135deg,#7c3aed,#6366f1); color:#ffffff; text-decoration:none; padding:13px 36px; border-radius:8px; font-weight:700; font-size:15px; display:inline-block;">
                            Sign In to NeuroScan
                          </a>
                        </div>
                      </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                      <td style="background:#f9fafb; border-top:1px solid #e5e7eb; padding:20px 40px; text-align:center;">
                        <p style="margin:0; color:#9ca3af; font-size:12px;">© 2026 NeuroScan · Medical-grade AI imaging platform</p>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
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
            <body style="margin:0; padding:0; background:#f3f4f6; font-family: Arial, Helvetica, sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6; padding:40px 0;">
                <tr><td align="center">
                  <table width="560" cellpadding="0" cellspacing="0" style="background:#ffffff; border-radius:12px; overflow:hidden; border:1px solid #e5e7eb;">
                    <!-- Header -->
                    <tr>
                      <td style="background:linear-gradient(135deg,#7c3aed,#6366f1); padding:32px 40px; text-align:center;">
                        <div style="font-size:36px; margin-bottom:12px;">🔐</div>
                        <h1 style="margin:0; color:#ffffff; font-size:22px; font-weight:700;">Password Reset Request</h1>
                      </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                      <td style="padding:36px 40px;">
                        <p style="margin:0 0 16px; color:#111827; font-size:15px; line-height:1.7;">Hi <strong>{firstName}</strong>,</p>
                        <p style="margin:0 0 24px; color:#374151; font-size:15px; line-height:1.7;">We received a request to reset your password. Use the 6-digit code below:</p>
                        <div style="text-align:center; margin:28px 0;">
                          <div style="display:inline-block; background:#f5f3ff; border:2px solid #c4b5fd; border-radius:12px; padding:20px 48px;">
                            <span style="font-size:40px; font-weight:700; color:#6d28d9; letter-spacing:10px;">{code}</span>
                          </div>
                        </div>
                        <div style="background:#fffbeb; border:1px solid #fde68a; border-radius:8px; padding:16px 20px; margin:24px 0;">
                          <p style="margin:0; color:#92400e; font-size:14px;">⏱️ This code is valid for <strong>15 minutes</strong>. If you did not request a password reset, you can safely ignore this email.</p>
                        </div>
                      </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                      <td style="background:#f9fafb; border-top:1px solid #e5e7eb; padding:20px 40px; text-align:center;">
                        <p style="margin:0; color:#9ca3af; font-size:12px;">© 2026 NeuroScan · Medical-grade AI imaging platform</p>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

    await SendEmailAsync(toEmail, subject, body);
  }

  public async Task SendScanResultsEmailAsync(string toEmail, string patientName, ScanResultEmailData data)
  {
    var subject = "NeuroScan – Your MRI Analysis Results Are Ready";

    var riskColor = data.EpilepsyRiskLevel switch
    {
      "High" => "#dc2626",
      "Moderate" => "#d97706",
      _ => "#16a34a"
    };
    var riskBg = data.EpilepsyRiskLevel switch
    {
      "High" => "#fef2f2",
      "Moderate" => "#fffbeb",
      _ => "#f0fdf4"
    };
    var riskBorder = data.EpilepsyRiskLevel switch
    {
      "High" => "#fecaca",
      "Moderate" => "#fde68a",
      _ => "#bbf7d0"
    };

    var body = $"""
            <!DOCTYPE html>
            <html>
            <body style="margin:0; padding:0; background:#f3f4f6; font-family: Arial, Helvetica, sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6; padding:40px 0;">
                <tr><td align="center">
                  <table width="620" cellpadding="0" cellspacing="0" style="background:#ffffff; border-radius:12px; overflow:hidden; border:1px solid #e5e7eb;">
                    <!-- Header -->
                    <tr>
                      <td style="background:linear-gradient(135deg,#0891b2,#0d9488); padding:32px 40px; text-align:center;">
                        <div style="font-size:36px; margin-bottom:12px;">🧠</div>
                        <h1 style="margin:0 0 6px; color:#ffffff; font-size:22px; font-weight:700;">MRI Analysis Complete</h1>
                        <p style="margin:0; color:rgba(255,255,255,0.85); font-size:14px;">Scan Date: {data.ScanDate:MMMM dd, yyyy}</p>
                      </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                      <td style="padding:36px 40px;">
                        <p style="margin:0 0 12px; color:#111827; font-size:15px; line-height:1.7;">Dear <strong>{patientName}</strong>,</p>
                        <p style="margin:0 0 28px; color:#374151; font-size:15px; line-height:1.7;">Your brain MRI has been analyzed using our SegResNet AI model. Below are the results. A full PDF report is attached.</p>

                        <!-- Epilepsy Risk Banner -->
                        <div style="background:{riskBg}; border:2px solid {riskBorder}; border-radius:10px; padding:20px 24px; margin-bottom:28px; display:flex; align-items:center;">
                          <div>
                            <p style="margin:0 0 4px; font-size:13px; color:#374151; text-transform:uppercase; letter-spacing:0.5px; font-weight:600;">Epilepsy Risk Assessment</p>
                            <p style="margin:0; font-size:22px; font-weight:700; color:{riskColor};">{data.EpilepsyRiskLevel} Risk &nbsp;<span style="font-size:15px; font-weight:400; color:#6b7280;">({data.EpilepsyRiskScore:F0}/100)</span></p>
                          </div>
                        </div>

                        <!-- Volumetrics -->
                        <p style="margin:0 0 8px; color:#0891b2; font-weight:700; font-size:13px; text-transform:uppercase; letter-spacing:0.5px;">SegResNet Volumetric Analysis</p>
                        <table width="100%" cellpadding="0" cellspacing="0" style="border:1px solid #e5e7eb; border-radius:8px; overflow:hidden; margin-bottom:28px; font-size:14px;">
                          <tr style="background:#f0fdfa;"><td style="padding:10px 14px; color:#374151;">CSF Volume</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.CsfVolume:F2} cm³</td></tr>
                          <tr><td style="padding:10px 14px; color:#374151;">Gray Matter Volume</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.GmVolume:F2} cm³</td></tr>
                          <tr style="background:#f0fdfa;"><td style="padding:10px 14px; color:#374151;">White Matter Volume</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.WmVolume:F2} cm³</td></tr>
                          <tr><td style="padding:10px 14px; color:#374151;">Asymmetry Index</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.AsymmetryIndex:F4}%</td></tr>
                        </table>

                        <!-- AI Medical Report -->
                        <p style="margin:0 0 10px; color:#111827; font-weight:700; font-size:15px;">🩺 AI Medical Interpretation</p>
                        <div style="background:#f8fafc; border-left:4px solid #0891b2; border-radius:0 8px 8px 0; padding:16px 20px; color:#374151; font-size:14px; line-height:1.8; white-space:pre-wrap;">{System.Net.WebUtility.HtmlEncode(data.MedicalReport)}</div>

                        <!-- Disclaimer -->
                        <div style="background:#fffbeb; border:1px solid #fde68a; border-radius:8px; padding:16px 20px; margin-top:24px;">
                          <p style="margin:0; color:#92400e; font-size:13px;">⚠️ This report is AI-generated and must be interpreted by a qualified neurologist alongside clinical history and EEG data. It does not replace professional medical advice.</p>
                        </div>
                      </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                      <td style="background:#f9fafb; border-top:1px solid #e5e7eb; padding:20px 40px; text-align:center;">
                        <p style="margin:0; color:#9ca3af; font-size:12px;">© 2026 NeuroScan · Medical-grade AI imaging platform</p>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

    // Generate PDF
    var pdfBytes = GenerateReportPdf(patientName, data);

    var message = new MimeMessage();
    message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
    message.To.Add(new MailboxAddress(string.Empty, toEmail));
    message.Subject = subject;

    var multipart = new Multipart("mixed");
    multipart.Add(new TextPart("html") { Text = body });

    var attachment = new MimePart("application", "pdf")
    {
      Content = new MimeContent(new MemoryStream(pdfBytes)),
      ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
      ContentTransferEncoding = ContentEncoding.Base64,
      FileName = $"NeuroScan_Report_{data.ScanDate:yyyy-MM-dd}.pdf"
    };
    multipart.Add(attachment);
    message.Body = multipart;

    using var client = new SmtpClient();
    await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
    await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword);
    await client.SendAsync(message);
    await client.DisconnectAsync(true);
  }

  private static byte[] GenerateReportPdf(string patientName, ScanResultEmailData data)
  {
    var riskColor = data.EpilepsyRiskLevel switch
    {
      "High" => "#dc2626",
      "Moderate" => "#d97706",
      _ => "#16a34a"
    };

    var doc = Document.Create(container =>
    {
      container.Page(page =>
      {
        page.Size(PageSizes.A4);
        page.Margin(40);
        page.DefaultTextStyle(t => t.FontSize(10).FontColor("#1e293b"));

        page.Header().Column(col =>
        {
          col.Item().Row(row =>
          {
            row.RelativeItem().Column(c =>
            {
              c.Item().Text("NeuroScan").FontSize(22).Bold().FontColor("#0891b2");
              c.Item().Text("AI Brain MRI Analysis Report — Epilepsy Assessment").FontSize(11).FontColor("#475569");
            });
            row.ConstantItem(130).AlignRight().Column(c =>
            {
              c.Item().Text($"Date: {data.ScanDate:yyyy-MM-dd}").FontSize(9).FontColor("#64748b");
              c.Item().Text($"Patient: {patientName}").FontSize(9).Bold();
            });
          });
          col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#e2e8f0");
        });

        page.Content().PaddingTop(20).Column(col =>
        {
          // Epilepsy Risk Banner
          col.Item().Background(data.EpilepsyRiskLevel == "High" ? "#fef2f2" : data.EpilepsyRiskLevel == "Moderate" ? "#fffbeb" : "#f0fdf4")
            .Border(1).BorderColor(data.EpilepsyRiskLevel == "High" ? "#fecaca" : data.EpilepsyRiskLevel == "Moderate" ? "#fde68a" : "#bbf7d0")
            .Padding(14).Column(b =>
          {
            b.Item().Text("EPILEPSY RISK ASSESSMENT").FontSize(9).Bold().FontColor("#6b7280");
            b.Item().PaddingTop(4).Text($"{data.EpilepsyRiskLevel.ToUpper()} RISK  ({data.EpilepsyRiskScore:F0}/100)").FontSize(16).Bold().FontColor(riskColor);
          });

          col.Item().PaddingTop(20).Text("SegResNet Volumetric Analysis").FontSize(13).Bold().FontColor("#0891b2");
          col.Item().PaddingTop(6).Table(t =>
          {
            t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
            void Row(string label, string value)
            {
              t.Cell().Padding(4).Text(label).FontColor("#475569");
              t.Cell().Padding(4).AlignRight().Text(value).Bold();
            }
            Row("CSF Volume", $"{data.CsfVolume:F2} cm³");
            Row("Gray Matter Volume", $"{data.GmVolume:F2} cm³");
            Row("White Matter Volume", $"{data.WmVolume:F2} cm³");
            Row("Asymmetry Index", $"{data.AsymmetryIndex:F4}%");
          });

          col.Item().PaddingTop(20).LineHorizontal(1).LineColor("#e2e8f0");

          col.Item().PaddingTop(16).Text("AI Medical Interpretation").FontSize(13).Bold().FontColor("#0f172a");
          col.Item().PaddingTop(8).Text(data.MedicalReport).FontSize(10).FontColor("#334155").LineHeight(1.6f);

          col.Item().PaddingTop(24).Background("#fffbeb").Padding(12).Border(1).BorderColor("#fde047").Column(warn =>
          {
            warn.Item().Text("Disclaimer").Bold().FontColor("#854d0e");
            warn.Item().PaddingTop(4).Text("This report is AI-generated and must be interpreted by a qualified neurologist alongside clinical history and EEG data. It does not replace professional medical advice.").FontSize(9).FontColor("#713f12");
          });
        });

        page.Footer().AlignCenter().Text(t =>
        {
          t.Span("© 2026 NeuroScan — Medical-grade AI imaging platform  |  Page ").FontSize(8).FontColor("#94a3b8");
          t.CurrentPageNumber().FontSize(8).FontColor("#94a3b8");
          t.Span(" of ").FontSize(8).FontColor("#94a3b8");
          t.TotalPages().FontSize(8).FontColor("#94a3b8");
        });
      });
    });

    return doc.GeneratePdf();
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
