namespace NeuroScan.Application.IServices;

public interface IOpenAiReportService
{
    Task<string> GenerateMedicalReportAsync(DualModelAnalysisResponseDTO analysisData, PatientContextDTO patientContext);
}

public class PatientContextDTO
{
    public string PatientName { get; set; } = string.Empty;
    public int Age { get; set; }
    public DateTime ScanDate { get; set; }
}
