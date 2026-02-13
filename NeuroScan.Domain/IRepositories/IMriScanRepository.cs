using NeuroScan.Domain.Entities;

namespace NeuroScan.Domain.IRepositories;

public interface IMriScanRepository
{
    Task<MriScan?> GetByIdAsync(Guid id);
    Task<IEnumerable<MriScan>> GetByPatientIdAsync(Guid patientId);
    Task<IEnumerable<MriScan>> GetPendingReviewScansAsync();
    Task<IEnumerable<MriScan>> GetByStatusAsync(ScanStatus status);
    Task AddAsync(MriScan scan);
    Task UpdateAsync(MriScan scan);
}
