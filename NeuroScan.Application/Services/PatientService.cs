using NeuroScan.Application.IServices;
using NeuroScan.Domain.Entities;
using NeuroScan.Domain.IRepositories;

namespace NeuroScan.Application.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;

    public PatientService(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task<PatientDTO?> GetByIdAsync(Guid patientId, Guid userId)
    {
        var patient = await _patientRepository.GetByIdAsync(patientId);

        if (patient == null || patient.CreatedByUserId != userId)
        {
            return null;
        }

        return MapToDTO(patient);
    }

    public async Task<IEnumerable<PatientDTO>> GetAllByUserAsync(Guid userId)
    {
        var patients = await _patientRepository.GetByUserIdAsync(userId);
        return patients.Select(MapToDTO);
    }

    public async Task<PatientDTO> CreatePatientAsync(CreatePatientDTO dto, Guid userId)
    {
        // Check if MRN already exists
        var existingPatient = await _patientRepository.GetByMedicalRecordNumberAsync(dto.MedicalRecordNumber);
        if (existingPatient != null)
        {
            throw new InvalidOperationException("A patient with this Medical Record Number already exists");
        }

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            MedicalRecordNumber = dto.MedicalRecordNumber,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _patientRepository.AddAsync(patient);
        return MapToDTO(patient);
    }

    public async Task<PatientDTO?> UpdatePatientAsync(Guid patientId, UpdatePatientDTO dto, Guid userId)
    {
        var patient = await _patientRepository.GetByIdAsync(patientId);

        if (patient == null || patient.CreatedByUserId != userId)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(dto.FirstName))
            patient.FirstName = dto.FirstName;

        if (!string.IsNullOrWhiteSpace(dto.LastName))
            patient.LastName = dto.LastName;

        if (dto.DateOfBirth.HasValue)
            patient.DateOfBirth = dto.DateOfBirth.Value;

        patient.UpdatedAt = DateTime.UtcNow;

        await _patientRepository.UpdateAsync(patient);
        return MapToDTO(patient);
    }

    private static PatientDTO MapToDTO(Patient patient)
    {
        return new PatientDTO
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            DateOfBirth = patient.DateOfBirth,
            MedicalRecordNumber = patient.MedicalRecordNumber,
            Age = CalculateAge(patient.DateOfBirth)
        };
    }

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }
}
