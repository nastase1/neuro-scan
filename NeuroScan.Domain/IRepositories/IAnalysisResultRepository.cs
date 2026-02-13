using NeuroScan.Domain.Entities;

namespace NeuroScan.Domain.IRepositories;

public interface IAnalysisResultRepository
{
    Task<AnalysisResult?> GetByMriScanIdAsync(Guid scanId);
    Task AddAsync(AnalysisResult result);
    Task UpdateAsync(AnalysisResult result);
}
