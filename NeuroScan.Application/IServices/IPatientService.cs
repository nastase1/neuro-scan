namespace NeuroScan.Application.IServices;

public interface IPatientService
{
    Task<PatientDTO?> GetByIdAsync(Guid patientId, Guid userId);
    Task<IEnumerable<PatientDTO>> GetAllByUserAsync(Guid userId);
    Task<PatientDTO?> GetMyPatientAsync(Guid userId);
    Task<PatientDTO> CreatePatientAsync(CreatePatientDTO dto, Guid userId);
    Task<PatientDTO?> UpdatePatientAsync(Guid patientId, UpdatePatientDTO dto, Guid userId);
}
