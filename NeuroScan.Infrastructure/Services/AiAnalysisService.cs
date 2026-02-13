using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using NeuroScan.Application.IServices;

namespace NeuroScan.Infrastructure.Services;

public class AiAnalysisService : IAiAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly string _pythonApiUrl;

    public AiAnalysisService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _pythonApiUrl = configuration["PythonAiService:Url"] ?? "http://python-ai:8000";
    }

    public async Task<AiAnalysisResponseDTO> AnalyzeMriScanAsync(string niiFilePath)
    {
        using var fileStream = File.OpenRead(niiFilePath);
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", Path.GetFileName(niiFilePath));

        var response = await _httpClient.PostAsync($"{_pythonApiUrl}/analyze", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AiAnalysisResponseDTO>();
        return result ?? throw new Exception("Failed to deserialize AI response");
    }
}
