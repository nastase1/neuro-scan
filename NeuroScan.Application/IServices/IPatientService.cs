using NeuroScan.Domain.Entities;

namespace NeuroScan.Application.IServices;

public interface IPatientService
{
    Task<PatientDTO?> GetByIdAsync(Guid patientId, Guid userId);
    Task<IEnumerable<PatientDTO>> GetAllByUserAsync(Guid userId);
    Task<PatientDTO> CreatePatientAsync(CreatePatientDTO dto, Guid userId);
    Task<PatientDTO?> UpdatePatientAsync(Guid patientId, UpdatePatientDTO dto, Guid userId);
}

public class PatientDTO
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class CreatePatientDTO
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public required string MedicalRecordNumber { get; set; }
}

public class UpdatePatientDTO
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
}
