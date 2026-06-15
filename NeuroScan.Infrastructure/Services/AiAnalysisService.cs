using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using NeuroScan.Application.IServices;
using NeuroScan.Application.DTOs;

namespace NeuroScan.Infrastructure.Services;

public class AiAnalysisService : IAiAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly string _pythonApiUrl;

    public AiAnalysisService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _pythonApiUrl = configuration["PythonAiService:Url"] ?? "http://localhost:8000";
    }

    public async Task<SegResNetAnalysisResponseDTO> AnalyzeMriScanAsync(string niiFilePath)
    {
        using var fileStream = File.OpenRead(niiFilePath);
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", Path.GetFileName(niiFilePath));

        var response = await _httpClient.PostAsync($"{_pythonApiUrl}/analyze", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SegResNetAnalysisResponseDTO>();
        return result ?? throw new Exception("Failed to deserialize AI response");
    }

    public async Task<SegResNetAnalysisResponseDTO> AnalyzeTumorScanAsync(string t1Path, string t1cePath, string t2Path, string flairPath)
    {
        using var t1Stream = File.OpenRead(t1Path);
        using var t1ceStream = File.OpenRead(t1cePath);
        using var t2Stream = File.OpenRead(t2Path);
        using var flairStream = File.OpenRead(flairPath);
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(t1Stream), "t1", Path.GetFileName(t1Path));
        content.Add(new StreamContent(t1ceStream), "t1ce", Path.GetFileName(t1cePath));
        content.Add(new StreamContent(t2Stream), "t2", Path.GetFileName(t2Path));
        content.Add(new StreamContent(flairStream), "flair", Path.GetFileName(flairPath));

        var response = await _httpClient.PostAsync($"{_pythonApiUrl}/analyze-tumor", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SegResNetAnalysisResponseDTO>();
        return result ?? throw new Exception("Failed to deserialize AI tumor response");
    }

    public async Task<List<string>> GetRawSlicesAsync(string niiFilePath)
    {
        using var fileStream = File.OpenRead(niiFilePath);
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", Path.GetFileName(niiFilePath));

        var response = await _httpClient.PostAsync($"{_pythonApiUrl}/raw-slices", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<RawSlicesResponseDTO>();
        return result?.Slices ?? new List<string>();
    }

    public async Task<MeshDataResponseDTO> Get3DMeshAsync(string niiFilePath)
    {
        using var fileStream = File.OpenRead(niiFilePath);
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", Path.GetFileName(niiFilePath));

        var response = await _httpClient.PostAsync($"{_pythonApiUrl}/analyze-3d", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MeshDataResponseDTO>();
        return result ?? throw new Exception("Failed to deserialize 3D mesh response");
    }

    public async Task<MeshDataResponseDTO> Get3DMeshTumorAsync(string t1Path, string t1cePath, string t2Path, string flairPath)
    {
        using var t1Stream = File.OpenRead(t1Path);
        using var t1ceStream = File.OpenRead(t1cePath);
        using var t2Stream = File.OpenRead(t2Path);
        using var flairStream = File.OpenRead(flairPath);
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(t1Stream), "t1", Path.GetFileName(t1Path));
        content.Add(new StreamContent(t1ceStream), "t1ce", Path.GetFileName(t1cePath));
        content.Add(new StreamContent(t2Stream), "t2", Path.GetFileName(t2Path));
        content.Add(new StreamContent(flairStream), "flair", Path.GetFileName(flairPath));

        var response = await _httpClient.PostAsync($"{_pythonApiUrl}/analyze-3d-tumor", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MeshDataResponseDTO>();
        return result ?? throw new Exception("Failed to deserialize 3D tumor mesh response");
    }
}

public class RawSlicesResponseDTO
{
    public bool Success { get; set; }
    public List<string> Slices { get; set; } = new();
    public int Count { get; set; }
}
