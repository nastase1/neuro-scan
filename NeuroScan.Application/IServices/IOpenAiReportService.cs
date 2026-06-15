using NeuroScan.Application.DTOs;

namespace NeuroScan.Application.IServices;

public interface IOpenAiReportService
{
    Task<string> GenerateMedicalReportAsync(SegResNetAnalysisResponseDTO analysisData, PatientContextDTO patientContext);
    Task<string> GenerateEvolutionReportAsync(string userPrompt, string systemPrompt);
}

public class PatientContextDTO
{
    public string PatientName { get; set; } = string.Empty;
    public int Age { get; set; }
    public DateTime ScanDate { get; set; }
}
