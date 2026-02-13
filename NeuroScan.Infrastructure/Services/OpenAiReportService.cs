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

    public async Task<string> GenerateMedicalReportAsync(AiAnalysisResponseDTO analysisData, PatientContextDTO patientContext)
    {
        var systemPrompt = @"You are a medical AI assistant specializing in neuroimaging analysis. 
Generate a professional medical report based on the provided MRI brain tissue volume measurements.
Include clinical interpretations and potential abnormalities if volumes deviate significantly from normal ranges.
Use medical terminology appropriate for physician review.";

        var userPrompt = $@"Patient: {patientContext.PatientName}, Age: {patientContext.Age}
Scan Date: {patientContext.ScanDate:yyyy-MM-dd}

Analysis Results:
- CSF Volume: {analysisData.CsfVolume:F2} cm³
- Gray Matter Volume: {analysisData.GmVolume:F2} cm³
- White Matter Volume: {analysisData.WmVolume:F2} cm³
- Brain Asymmetry Index: {analysisData.AsymmetryIndex:F4}

Generate a medical report.";

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
