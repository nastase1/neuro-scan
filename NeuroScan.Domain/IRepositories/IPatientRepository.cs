using NeuroScan.Domain.Entities;

namespace NeuroScan.Domain.IRepositories;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id);
    Task<IEnumerable<Patient>> GetAllAsync();
    Task<IEnumerable<Patient>> GetByUserIdAsync(Guid userId);
    Task<Patient?> GetByMedicalRecordNumberAsync(string mrn);
    Task<Patient?> GetByPatientUserIdAsync(Guid userId);
    Task AddAsync(Patient patient);
    Task UpdateAsync(Patient patient);
}
