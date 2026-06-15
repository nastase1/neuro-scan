using Moq;
using NeuroScan.Application.IServices;
using NeuroScan.Application.DTOs;
using NeuroScan.Application.Services;
using NeuroScan.Domain.Entities;
using NeuroScan.Domain.IRepositories;

namespace NeuroScan.Tests.Services;

public class PatientServiceTests
{
    private readonly Mock<IPatientRepository> _patientRepo;
    private readonly PatientService _sut;

    public PatientServiceTests()
    {
        _patientRepo = new Mock<IPatientRepository>();
        _sut = new PatientService(_patientRepo.Object);
    }

    // ── GetByIdAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenPatientExistsAndUserMatches_ReturnsMappedDTO()
    {
        var patientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = DateTime.Today.AddYears(-30),
            MedicalRecordNumber = "MRN-001",
            CreatedByUserId = userId
        };

        _patientRepo.Setup(r => r.GetByIdAsync(patientId)).ReturnsAsync(patient);

        var result = await _sut.GetByIdAsync(patientId, userId);

        Assert.NotNull(result);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("MRN-001", result.MedicalRecordNumber);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPatientNotFound_ReturnsNull()
    {
        _patientRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Patient?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotMatch_ReturnsNull()
    {
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            FirstName = "J", LastName = "D",
            DateOfBirth = DateTime.Today.AddYears(-20),
            MedicalRecordNumber = "MRN-002",
            CreatedByUserId = Guid.NewGuid()
        };

        _patientRepo.Setup(r => r.GetByIdAsync(patientId)).ReturnsAsync(patient);

        var result = await _sut.GetByIdAsync(patientId, Guid.NewGuid());

        Assert.Null(result);
    }

    // ── GetAllByUserAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllByUserAsync_ReturnsAllPatientsForUser()
    {
        var userId = Guid.NewGuid();
        var patients = new List<Patient>
        {
            new() { Id = Guid.NewGuid(), FirstName = "A", LastName = "A", DateOfBirth = DateTime.Today.AddYears(-10), MedicalRecordNumber = "M1", CreatedByUserId = userId },
            new() { Id = Guid.NewGuid(), FirstName = "B", LastName = "B", DateOfBirth = DateTime.Today.AddYears(-20), MedicalRecordNumber = "M2", CreatedByUserId = userId }
        };

        _patientRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(patients);

        var result = (await _sut.GetAllByUserAsync(userId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.MedicalRecordNumber == "M1");
        Assert.Contains(result, p => p.MedicalRecordNumber == "M2");
    }

    // ── CreatePatientAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePatientAsync_WithExistingMrn_ThrowsInvalidOperationException()
    {
        var existingPatient = new Patient { Id = Guid.NewGuid(), FirstName = "X", LastName = "X", DateOfBirth = DateTime.Today, MedicalRecordNumber = "DUPE-001", CreatedByUserId = Guid.NewGuid() };

        _patientRepo.Setup(r => r.GetByMedicalRecordNumberAsync("DUPE-001")).ReturnsAsync(existingPatient);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.CreatePatientAsync(new CreatePatientDTO
            {
                FirstName = "New", LastName = "Patient",
                DateOfBirth = DateTime.Today.AddYears(-25),
                MedicalRecordNumber = "DUPE-001"
            }, Guid.NewGuid()));
    }

    [Fact]
    public async Task CreatePatientAsync_WithNewMrn_CreatesAndReturnsDTO()
    {
        var userId = Guid.NewGuid();
        _patientRepo.Setup(r => r.GetByMedicalRecordNumberAsync("NEW-001")).ReturnsAsync((Patient?)null);
        _patientRepo.Setup(r => r.AddAsync(It.IsAny<Patient>())).Returns(Task.CompletedTask);

        var result = await _sut.CreatePatientAsync(new CreatePatientDTO
        {
            FirstName = "Alice", LastName = "Smith",
            DateOfBirth = DateTime.Today.AddYears(-35),
            MedicalRecordNumber = "NEW-001",
            Email = "alice@test.com"
        }, userId);

        Assert.NotNull(result);
        Assert.Equal("Alice", result.FirstName);
        Assert.Equal("NEW-001", result.MedicalRecordNumber);
        _patientRepo.Verify(r => r.AddAsync(It.IsAny<Patient>()), Times.Once);
    }

    // ── UpdatePatientAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePatientAsync_WhenPatientNotFound_ReturnsNull()
    {
        _patientRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Patient?)null);

        var result = await _sut.UpdatePatientAsync(Guid.NewGuid(), new UpdatePatientDTO { FirstName = "X" }, Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePatientAsync_WithValidData_UpdatesFields()
    {
        var patientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId, FirstName = "Old", LastName = "Name",
            DateOfBirth = DateTime.Today.AddYears(-20),
            MedicalRecordNumber = "MRN-UP", CreatedByUserId = userId
        };

        _patientRepo.Setup(r => r.GetByIdAsync(patientId)).ReturnsAsync(patient);
        _patientRepo.Setup(r => r.UpdateAsync(It.IsAny<Patient>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdatePatientAsync(patientId,
            new UpdatePatientDTO { FirstName = "New", Email = "new@test.com" }, userId);

        Assert.NotNull(result);
        Assert.Equal("New", result.FirstName);
        Assert.Equal("new@test.com", result.Email);
        _patientRepo.Verify(r => r.UpdateAsync(It.IsAny<Patient>()), Times.Once);
    }
}
