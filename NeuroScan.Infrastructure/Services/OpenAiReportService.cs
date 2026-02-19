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

    public async Task<string> GenerateMedicalReportAsync(DualModelAnalysisResponseDTO analysisData, PatientContextDTO patientContext)
    {
        // Modificăm System Prompt-ul pentru a include expertiza în epilepsie
        var systemPrompt = @"You are a specialized neuroradiology AI assistant. 
Your goal is to generate a clinical report focusing on structural biomarkers associated with epilepsy, such as regional atrophy or significant hemispheric asymmetry.
You will compare data from two segmentation models (UNet and SegResNet).
Analyze Gray Matter (GM), White Matter (WM), and Cerebrospinal Fluid (CSF) volumes.
Pay special attention to the Brain Asymmetry Index and Gray Matter volume discrepancies, as these can indicate potential epileptogenic zones or hippocampal sclerosis.
IMPORTANT: Provide a clinical correlation section regarding epilepsy risk, but maintain a professional tone, noting that findings must be correlated with EEG and clinical symptoms.";

        var comparisonText = analysisData.Comparison.DisagreementPercentage < 5
            ? $"The models show excellent agreement ({analysisData.Comparison.Confidence:F1}% confidence)."
            : $"The models show a discrepancy of {analysisData.Comparison.DisagreementPercentage:F1}%. Higher caution is advised for focal asymmetry detection.";

        // Îmbogățim User Prompt-ul cu instrucțiuni specifice pentru epilepsie
        var userPrompt = $@"Patient: {patientContext.PatientName}, Age: {patientContext.Age}
Scan Date: {patientContext.ScanDate:yyyy-MM-dd}

=== DUAL-MODEL ANALYSIS RESULTS ===
Model 1 ({analysisData.Model1.Name}): GM: {analysisData.Model1.GmVolume:F2}cm³, WM: {analysisData.Model1.WmVolume:F2}cm³, Asymmetry Index: {analysisData.Model1.AsymmetryIndex:F4}
Model 2 ({analysisData.Model2.Name}): GM: {analysisData.Model2.GmVolume:F2}cm³, WM: {analysisData.Model2.WmVolume:F2}cm³, Asymmetry Index: {analysisData.Model2.AsymmetryIndex:F4}

=== COMPARISON DATA ===
Dice Scores (GM): {analysisData.Comparison.DiceScores.Gm:F4}
Volume Differences (GM): {analysisData.Comparison.VolumeDifferences.Gm:F2} cm³
Recommended Model: {analysisData.Comparison.RecommendedModel}

Please generate a structured report including:
1. SUMMARY OF VOLUMETRIC FINDINGS: (Focus on Gray Matter and Asymmetry).
2. ASYMMETRY ANALYSIS: Does the Brain Asymmetry Index ({analysisData.Model1.AsymmetryIndex:F4}) suggest potential focal cortical dysplasia or hippocampal issues?
3. EPILEPSY CORRELATION: Based on the volumetric data and asymmetry, identify if there are structural patterns consistent with epilepsy (e.g., significant GM loss or abnormal asymmetry).
4. RELIABILITY: Comment on the agreement between {analysisData.Model1.Name} and {analysisData.Model2.Name}.
5. CLINICAL RECOMMENDATION: Suggest if further targeted imaging (like 3T MRI with epilepsy protocol) or EEG correlation is warranted.";

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
}
