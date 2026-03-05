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

    // Build HTML email body
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
                        <p style="margin:0; color:rgba(255,255,255,0.8); font-size:14px;">Scan Date: {data.ScanDate:MMMM dd, yyyy}</p>
                      </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                      <td style="padding:36px 40px;">
                        <p style="margin:0 0 12px; color:#111827; font-size:15px; line-height:1.7;">Dear <strong>{patientName}</strong>,</p>
                        <p style="margin:0 0 24px; color:#374151; font-size:15px; line-height:1.7;">Your brain MRI scan has been analyzed by our AI. Below are the results. A full PDF report is attached to this email.</p>

                        <!-- Model 1 -->
                        <p style="margin:0 0 8px; color:#0891b2; font-weight:700; font-size:14px; text-transform:uppercase; letter-spacing:0.5px;">Model 1 – UNet Segmentation</p>
                        <table width="100%" cellpadding="0" cellspacing="0" style="border:1px solid #e5e7eb; border-radius:8px; overflow:hidden; margin-bottom:20px; font-size:14px;">
                          <tr style="background:#f0fdfa;"><td style="padding:10px 14px; color:#374151;">CSF Volume</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.CsfVolume:F2} mL</td></tr>
                          <tr><td style="padding:10px 14px; color:#374151;">Grey Matter Volume</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.GmVolume:F2} mL</td></tr>
                          <tr style="background:#f0fdfa;"><td style="padding:10px 14px; color:#374151;">White Matter Volume</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.WmVolume:F2} mL</td></tr>
                          <tr><td style="padding:10px 14px; color:#374151;">Asymmetry Index</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.AsymmetryIndex:F4}</td></tr>
                        </table>

                        <!-- Model 2 -->
                        <p style="margin:0 0 8px; color:#0d9488; font-weight:700; font-size:14px; text-transform:uppercase; letter-spacing:0.5px;">Model 2 – SegResNet Segmentation</p>
                        <table width="100%" cellpadding="0" cellspacing="0" style="border:1px solid #e5e7eb; border-radius:8px; overflow:hidden; margin-bottom:20px; font-size:14px;">
                          <tr style="background:#f0fdf4;"><td style="padding:10px 14px; color:#374151;">CSF Volume</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.CsfVolumeModel2:F2} mL</td></tr>
                          <tr><td style="padding:10px 14px; color:#374151;">Grey Matter Volume</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.GmVolumeModel2:F2} mL</td></tr>
                          <tr style="background:#f0fdf4;"><td style="padding:10px 14px; color:#374151;">White Matter Volume</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.WmVolumeModel2:F2} mL</td></tr>
                          <tr><td style="padding:10px 14px; color:#374151;">Asymmetry Index</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.AsymmetryIndexModel2:F4}</td></tr>
                        </table>

                        <!-- Comparison -->
                        <p style="margin:0 0 8px; color:#7c3aed; font-weight:700; font-size:14px; text-transform:uppercase; letter-spacing:0.5px;">Model Comparison</p>
                        <table width="100%" cellpadding="0" cellspacing="0" style="border:1px solid #e5e7eb; border-radius:8px; overflow:hidden; margin-bottom:24px; font-size:14px;">
                          <tr style="background:#faf5ff;"><td style="padding:10px 14px; color:#374151;">Dice Score – CSF</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.DiceScoreCsf:F4}</td></tr>
                          <tr><td style="padding:10px 14px; color:#374151;">Dice Score – GM</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.DiceScoreGm:F4}</td></tr>
                          <tr style="background:#faf5ff;"><td style="padding:10px 14px; color:#374151;">Dice Score – WM</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.DiceScoreWm:F4}</td></tr>
                          <tr><td style="padding:10px 14px; color:#374151;">Disagreement</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.DisagreementPercentage:F2}%</td></tr>
                          <tr style="background:#faf5ff;"><td style="padding:10px 14px; color:#374151;">Recommended Model</td><td style="padding:10px 14px; color:#0891b2; font-weight:700; text-align:right;">{data.RecommendedModel}</td></tr>
                          <tr><td style="padding:10px 14px; color:#374151;">Confidence</td><td style="padding:10px 14px; color:#111827; font-weight:600; text-align:right;">{data.ModelConfidence:F1}%</td></tr>
                        </table>

                        <!-- AI Medical Report -->
                        <p style="margin:0 0 10px; color:#111827; font-weight:700; font-size:15px;">🩺 AI Medical Interpretation</p>
                        <div style="background:#f8fafc; border-left:4px solid #0891b2; border-radius:0 8px 8px 0; padding:16px 20px; color:#374151; font-size:14px; line-height:1.8; white-space:pre-wrap;">{System.Net.WebUtility.HtmlEncode(data.MedicalReport)}</div>

                        <!-- Disclaimer -->
                        <div style="background:#fffbeb; border:1px solid #fde68a; border-radius:8px; padding:16px 20px; margin-top:24px;">
                          <p style="margin:0; color:#92400e; font-size:13px;">⚠️ This report is generated by AI and is intended to assist your physician. It does not replace professional medical advice. Please consult your doctor to discuss these results.</p>
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

    // Send with attachment
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
                          c.Item().Text("AI Brain MRI Analysis Report").FontSize(12).FontColor("#475569");
                        });
                      row.ConstantItem(120).AlignRight().Column(c =>
                        {
                          c.Item().Text($"Date: {data.ScanDate:yyyy-MM-dd}").FontSize(9).FontColor("#64748b");
                          c.Item().Text($"Patient: {patientName}").FontSize(9).Bold();
                        });
                    });
                  col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#e2e8f0");
                });

            page.Content().PaddingTop(20).Column(col =>
                {
                  // Model 1
                  col.Item().Text("Model 1 — UNet Segmentation").FontSize(13).Bold().FontColor("#0891b2");
                  col.Item().PaddingTop(6).Table(t =>
                    {
                      t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                      void Row(string label, string value)
                      {
                        t.Cell().Padding(4).Text(label).FontColor("#475569");
                        t.Cell().Padding(4).AlignRight().Text(value).Bold();
                      }
                      Row("CSF Volume", $"{data.CsfVolume:F2} mL");
                      Row("Grey Matter Volume", $"{data.GmVolume:F2} mL");
                      Row("White Matter Volume", $"{data.WmVolume:F2} mL");
                      Row("Asymmetry Index", $"{data.AsymmetryIndex:F4}");
                    });

                  col.Item().PaddingTop(16).Text("Model 2 — SegResNet Segmentation").FontSize(13).Bold().FontColor("#0d9488");
                  col.Item().PaddingTop(6).Table(t =>
                    {
                      t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                      void Row(string label, string value)
                      {
                        t.Cell().Padding(4).Text(label).FontColor("#475569");
                        t.Cell().Padding(4).AlignRight().Text(value).Bold();
                      }
                      Row("CSF Volume", $"{data.CsfVolumeModel2:F2} mL");
                      Row("Grey Matter Volume", $"{data.GmVolumeModel2:F2} mL");
                      Row("White Matter Volume", $"{data.WmVolumeModel2:F2} mL");
                      Row("Asymmetry Index", $"{data.AsymmetryIndexModel2:F4}");
                    });

                  col.Item().PaddingTop(16).Text("Model Comparison").FontSize(13).Bold().FontColor("#7c3aed");
                  col.Item().PaddingTop(6).Table(t =>
                    {
                      t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                      void Row(string label, string value)
                      {
                        t.Cell().Padding(4).Text(label).FontColor("#475569");
                        t.Cell().Padding(4).AlignRight().Text(value).Bold();
                      }
                      Row("Dice Score — CSF", $"{data.DiceScoreCsf:F4}");
                      Row("Dice Score — GM", $"{data.DiceScoreGm:F4}");
                      Row("Dice Score — WM", $"{data.DiceScoreWm:F4}");
                      Row("Disagreement", $"{data.DisagreementPercentage:F2}%");
                      Row("Recommended Model", data.RecommendedModel);
                      Row("Confidence", $"{data.ModelConfidence:F1}%");
                    });

                  col.Item().PaddingTop(20).LineHorizontal(1).LineColor("#e2e8f0");

                  col.Item().PaddingTop(16).Text("AI Medical Interpretation").FontSize(13).Bold().FontColor("#0f172a");
                  col.Item().PaddingTop(8).Text(data.MedicalReport).FontSize(10).FontColor("#334155").LineHeight(1.6f);

                  col.Item().PaddingTop(24).Background("#fef9c3").Padding(12).Border(1).BorderColor("#fde047").Column(warn =>
                    {
                      warn.Item().Text("Disclaimer").Bold().FontColor("#854d0e");
                      warn.Item().PaddingTop(4).Text("This report is generated by AI and is intended to assist a qualified physician. It does not replace professional medical advice. Please consult your doctor to discuss these results.").FontSize(9).FontColor("#713f12");
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
