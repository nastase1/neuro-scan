namespace NeuroScan.Application.IServices;

public interface IScanProcessingService
{
    void StartProcessingScan(Guid scanId);
    void StartProcessingTumorScan(Guid scanId);
}
