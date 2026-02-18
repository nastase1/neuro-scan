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
        var systemPrompt = @"You are a medical AI assistant specializing in neuroimaging analysis. 
Generate a professional medical report based on dual-model MRI brain tissue volume measurements from two independent AI models (UNet and SegResNet).
Compare the results, assess model agreement using Dice scores, and provide clinical interpretations.
Include potential abnormalities if volumes deviate significantly from normal ranges.
Use medical terminology appropriate for physician review.
Consider the recommended model and confidence score when making clinical conclusions.";

        var comparisonText = analysisData.Comparison.DisagreementPercentage < 5
            ? $"The models show excellent agreement ({analysisData.Comparison.Confidence:F1}% confidence)."
            : analysisData.Comparison.DisagreementPercentage < 10
                ? $"The models show good agreement ({analysisData.Comparison.Confidence:F1}% confidence)."
                : $"The models show moderate disagreement ({analysisData.Comparison.DisagreementPercentage:F1}% disagreement). Clinical review recommended.";

        var userPrompt = $@"Patient: {patientContext.PatientName}, Age: {patientContext.Age}
Scan Date: {patientContext.ScanDate:yyyy-MM-dd}

=== DUAL-MODEL ANALYSIS RESULTS ===

Model 1 ({analysisData.Model1.Name}) - Processing Time: {analysisData.Model1.ProcessingTime:F2}s:
- CSF Volume: {analysisData.Model1.CsfVolume:F2} cm³
- Gray Matter Volume: {analysisData.Model1.GmVolume:F2} cm³
- White Matter Volume: {analysisData.Model1.WmVolume:F2} cm³
- Brain Asymmetry Index: {analysisData.Model1.AsymmetryIndex:F4}

Model 2 ({analysisData.Model2.Name}) - Processing Time: {analysisData.Model2.ProcessingTime:F2}s:
- CSF Volume: {analysisData.Model2.CsfVolume:F2} cm³
- Gray Matter Volume: {analysisData.Model2.GmVolume:F2} cm³
- White Matter Volume: {analysisData.Model2.WmVolume:F2} cm³
- Brain Asymmetry Index: {analysisData.Model2.AsymmetryIndex:F4}

=== MODEL COMPARISON ===
Dice Scores (Agreement Metrics):
- CSF Dice Score: {analysisData.Comparison.DiceScores.Csf:F4}
- Gray Matter Dice Score: {analysisData.Comparison.DiceScores.Gm:F4}
- White Matter Dice Score: {analysisData.Comparison.DiceScores.Wm:F4}

Volume Differences:
- CSF Difference: {analysisData.Comparison.VolumeDifferences.Csf:F2} cm³
- Gray Matter Difference: {analysisData.Comparison.VolumeDifferences.Gm:F2} cm³
- White Matter Difference: {analysisData.Comparison.VolumeDifferences.Wm:F2} cm³

Overall Disagreement: {analysisData.Comparison.DisagreementPercentage:F1}%
Recommended Model: {analysisData.Comparison.RecommendedModel}
Model Confidence: {analysisData.Comparison.Confidence:F1}%

{comparisonText}

Generate a comprehensive medical report that:
1. Summarizes findings from both models
2. Assesses the reliability based on model agreement
3. Highlights any significant volume differences or asymmetry
4. Provides clinical interpretation considering the confidence level";

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
