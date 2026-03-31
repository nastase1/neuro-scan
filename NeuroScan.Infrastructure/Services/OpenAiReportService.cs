using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NeuroScan.Application.IServices;

namespace NeuroScan.Infrastructure.Services;

public class OpenAiReportService : IOpenAiReportService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAiReportService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"] ?? throw new Exception("OpenAI API key not configured");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<string> GenerateMedicalReportAsync(SegResNetAnalysisResponseDTO analysisData, PatientContextDTO patientContext)
    {
        var systemPrompt = @"You are a specialized neuroradiology AI assistant with expertise in epilepsy neuroimaging.
Your role is to analyze brain MRI volumetric data produced by SegResNet segmentation and generate a structured clinical report.
Focus on structural biomarkers associated with epilepsy: hemispheric asymmetry, gray matter volume loss, and CSF/GM ratio.
Key biomarkers to assess:
- Brain Asymmetry Index > 5% may indicate focal cortical dysplasia, hippocampal sclerosis, or an epileptogenic zone.
- Reduced gray matter volume may indicate cortical atrophy.
- Elevated CSF relative to brain tissue suggests parenchymal volume loss.
Always maintain a professional clinical tone and emphasize that findings must be correlated with EEG, clinical history, and neurological examination.";

        var riskFactorsText = string.Join("\n- ", analysisData.Epilepsy.Factors);

        var userPrompt = $@"Patient: {patientContext.PatientName}, Age: {patientContext.Age}
Scan Date: {patientContext.ScanDate:yyyy-MM-dd}

=== SEGRESNET VOLUMETRIC ANALYSIS ===
CSF Volume:          {analysisData.Segresnet.CsfVolume:F2} cm3
Gray Matter Volume:  {analysisData.Segresnet.GmVolume:F2} cm3
White Matter Volume: {analysisData.Segresnet.WmVolume:F2} cm3
Asymmetry Index:     {analysisData.Segresnet.AsymmetryIndex:F4}%

=== AUTOMATED EPILEPSY RISK ASSESSMENT ===
Risk Level: {analysisData.Epilepsy.RiskLevel} ({analysisData.Epilepsy.RiskScore:F1}/100)
Detected factors:
- {riskFactorsText}

Please generate a structured clinical report with the following sections:
1. VOLUMETRIC FINDINGS
2. ASYMMETRY ANALYSIS
3. EPILEPSY RISK ASSESSMENT
4. CLINICAL RECOMMENDATION
5. DISCLAIMER";

        var requestBody = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.3
        };

        var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var openAiResponse = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

        return openAiResponse
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "Report generation failed";
    }

    public async Task<string> GenerateEvolutionReportAsync(string userPrompt, string systemPrompt)
    {
        var requestBody = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.3
        };

        var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var openAiResponse = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

        return openAiResponse
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "Evolution report generation failed";
    }
}
