using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NeuroScan.Application.IServices;
using NeuroScan.Application.Services;
using NeuroScan.Domain.Entities;
using NeuroScan.Domain.IRepositories;

namespace NeuroScan.Tests.Services;

public class MriScanServiceTests
{
    private readonly Mock<IMriScanRepository> _mriScanRepo;
    private readonly Mock<IPatientRepository> _patientRepo;
    private readonly Mock<IUserRepository> _userRepo;
    private readonly Mock<IAnalysisResultRepository> _analysisResultRepo;
    private readonly Mock<IAiAnalysisService> _aiService;
    private readonly Mock<IOpenAiReportService> _openAiService;
    private readonly Mock<IScanProcessingService> _processingService;
    private readonly Mock<IMriImageService> _imageService;
    private readonly Mock<ILogger<MriScanService>> _logger;
    private readonly MriScanService _sut;

    public MriScanServiceTests()
    {
        _mriScanRepo = new Mock<IMriScanRepository>();
        _patientRepo = new Mock<IPatientRepository>();
        _userRepo = new Mock<IUserRepository>();
        _analysisResultRepo = new Mock<IAnalysisResultRepository>();
        _aiService = new Mock<IAiAnalysisService>();
        _openAiService = new Mock<IOpenAiReportService>();
        _processingService = new Mock<IScanProcessingService>();
        _imageService = new Mock<IMriImageService>();
        _logger = new Mock<ILogger<MriScanService>>();

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Storage:UploadPath"]).Returns(Path.GetTempPath());
        configMock.Setup(c => c["Storage:TrainingDataPath"]).Returns(Path.GetTempPath());

        _sut = new MriScanService(
            _mriScanRepo.Object,
            _patientRepo.Object,
            _userRepo.Object,
            _analysisResultRepo.Object,
            _aiService.Object,
            _openAiService.Object,
            _processingService.Object,
            _imageService.Object,
            _logger.Object,
            configMock.Object);
    }

    // ── GetScanDetailsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetScanDetailsAsync_WhenScanNotFound_ReturnsNull()
    {
        _mriScanRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((MriScan?)null);

        var result = await _sut.GetScanDetailsAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetScanDetailsAsync_WhenPatientNotFound_ReturnsNull()
    {
        var scan = new MriScan { Id = Guid.NewGuid(), OriginalFileName = "test.nii", StoredFilePath = "/tmp/test.nii", PatientId = Guid.NewGuid() };

        _mriScanRepo.Setup(r => r.GetByIdAsync(scan.Id)).ReturnsAsync(scan);
        _patientRepo.Setup(r => r.GetByIdAsync(scan.PatientId)).ReturnsAsync((Patient?)null);

        var result = await _sut.GetScanDetailsAsync(scan.Id, Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetScanDetailsAsync_WhenDoctorAccessesSomeoneElsesPatient_ReturnsNull()
    {
        var doctorId = Guid.NewGuid();
        var otherDoctorId = Guid.NewGuid();
        var scan = new MriScan { Id = Guid.NewGuid(), OriginalFileName = "t.nii", StoredFilePath = "/tmp/t.nii", PatientId = Guid.NewGuid() };
        var patient = new Patient
        {
            Id = scan.PatientId, FirstName = "P", LastName = "P",
            DateOfBirth = DateTime.Today, MedicalRecordNumber = "M",
            CreatedByUserId = otherDoctorId
        };

        _mriScanRepo.Setup(r => r.GetByIdAsync(scan.Id)).ReturnsAsync(scan);
        _patientRepo.Setup(r => r.GetByIdAsync(scan.PatientId)).ReturnsAsync(patient);

        var result = await _sut.GetScanDetailsAsync(scan.Id, doctorId, isDoctor: true);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetScanDetailsAsync_WhenDoctorOwnsPatient_ReturnsScanDetail()
    {
        var doctorId = Guid.NewGuid();
        var scanId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        var scan = new MriScan
        {
            Id = scanId, OriginalFileName = "scan.nii", StoredFilePath = "/tmp/scan.nii",
            PatientId = patientId, UploadDate = DateTime.UtcNow, Status = ScanStatus.Analyzed
        };
        var patient = new Patient
        {
            Id = patientId, FirstName = "Jane", LastName = "Smith",
            DateOfBirth = DateTime.Today.AddYears(-40), MedicalRecordNumber = "MRN-DOC",
            CreatedByUserId = doctorId
        };

        _mriScanRepo.Setup(r => r.GetByIdAsync(scanId)).ReturnsAsync(scan);
        _patientRepo.Setup(r => r.GetByIdAsync(patientId)).ReturnsAsync(patient);
        _analysisResultRepo.Setup(r => r.GetByMriScanIdAsync(scanId)).ReturnsAsync((AnalysisResult?)null);

        var result = await _sut.GetScanDetailsAsync(scanId, doctorId, isDoctor: true);

        Assert.NotNull(result);
        Assert.Equal(scanId, result.Id);
        Assert.Equal("Jane Smith", result.Patient.FullName);
    }

    [Fact]
    public async Task GetScanDetailsAsync_WhenStandardUserAccessesOwnScan_ReturnsScanDetail()
    {
        var userId = Guid.NewGuid();
        var scanId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        var scan = new MriScan
        {
            Id = scanId, OriginalFileName = "scan.nii", StoredFilePath = "/tmp/scan.nii",
            PatientId = patientId, UploadDate = DateTime.UtcNow, Status = ScanStatus.Analyzed
        };
        var patient = new Patient
        {
            Id = patientId, FirstName = "Self", LastName = "User",
            DateOfBirth = DateTime.Today.AddYears(-25), MedicalRecordNumber = "SELF-001",
            CreatedByUserId = Guid.NewGuid(),
            UserId = userId
        };

        _mriScanRepo.Setup(r => r.GetByIdAsync(scanId)).ReturnsAsync(scan);
        _patientRepo.Setup(r => r.GetByIdAsync(patientId)).ReturnsAsync(patient);
        _analysisResultRepo.Setup(r => r.GetByMriScanIdAsync(scanId)).ReturnsAsync((AnalysisResult?)null);

        var result = await _sut.GetScanDetailsAsync(scanId, userId, isDoctor: false);

        Assert.NotNull(result);
        Assert.Equal(scanId, result.Id);
    }

    // ── GetScansByPatientIdAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetScansByPatientIdAsync_WhenPatientNotFound_ThrowsUnauthorized()
    {
        _patientRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Patient?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.GetScansByPatientIdAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task GetScansByPatientIdAsync_WhenDoctorDoesNotOwnPatient_ThrowsUnauthorized()
    {
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId, FirstName = "P", LastName = "P",
            DateOfBirth = DateTime.Today, MedicalRecordNumber = "M",
            CreatedByUserId = Guid.NewGuid()
        };

        _patientRepo.Setup(r => r.GetByIdAsync(patientId)).ReturnsAsync(patient);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.GetScansByPatientIdAsync(patientId, Guid.NewGuid(), isDoctor: true));
    }

    // ── SubmitReviewAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitReviewAsync_WhenScanNotFound_ThrowsArgumentException()
    {
        _mriScanRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((MriScan?)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.SubmitReviewAsync(Guid.NewGuid(), Guid.NewGuid(), true, "notes"));
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenNoAnalysisResult_ThrowsInvalidOperationException()
    {
        var scanId = Guid.NewGuid();
        var scan = new MriScan { Id = scanId, OriginalFileName = "s.nii", StoredFilePath = "/tmp/s.nii" };

        _mriScanRepo.Setup(r => r.GetByIdAsync(scanId)).ReturnsAsync(scan);
        _analysisResultRepo.Setup(r => r.GetByMriScanIdAsync(scanId)).ReturnsAsync((AnalysisResult?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.SubmitReviewAsync(scanId, Guid.NewGuid(), true, "notes"));
    }

    [Fact]
    public async Task SubmitReviewAsync_WithValidData_UpdatesScanAndAnalysis()
    {
        var scanId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var scan = new MriScan { Id = scanId, OriginalFileName = "s.nii", StoredFilePath = "/tmp/s.nii" };
        var analysisResult = new AnalysisResult { Id = Guid.NewGuid(), MriScanId = scanId, EpilepsyRiskLevel = "Low" };

        _mriScanRepo.Setup(r => r.GetByIdAsync(scanId)).ReturnsAsync(scan);
        _analysisResultRepo.Setup(r => r.GetByMriScanIdAsync(scanId)).ReturnsAsync(analysisResult);
        _analysisResultRepo.Setup(r => r.UpdateAsync(It.IsAny<AnalysisResult>())).Returns(Task.CompletedTask);
        _mriScanRepo.Setup(r => r.UpdateAsync(It.IsAny<MriScan>())).Returns(Task.CompletedTask);

        await _sut.SubmitReviewAsync(scanId, doctorId, approved: true, notes: "Looks good");

        _analysisResultRepo.Verify(r => r.UpdateAsync(It.Is<AnalysisResult>(a => a.DoctorApproved == true && a.DoctorReviewNotes == "Looks good")), Times.Once);
        _mriScanRepo.Verify(r => r.UpdateAsync(It.Is<MriScan>(s => s.Status == ScanStatus.ReviewedByDoctor && s.ReviewedByDoctorId == doctorId)), Times.Once);
    }

    // ── UploadAndProcessScanAsync ──────────────────────────────────────────────

    [Fact]
    public async Task UploadAndProcessScanAsync_WhenPatientNotFound_ThrowsUnauthorized()
    {
        _patientRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Patient?)null);

        var mockFile = new Mock<IFormFile>();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.UploadAndProcessScanAsync(
                new MriScanUploadDTO { PatientId = Guid.NewGuid(), File = mockFile.Object },
                Guid.NewGuid()));
    }

    [Fact]
    public async Task UploadAndProcessScanAsync_WhenPatientBelongsToDifferentDoctor_ThrowsUnauthorized()
    {
        var patientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId, FirstName = "P", LastName = "P",
            DateOfBirth = DateTime.Today, MedicalRecordNumber = "M",
            CreatedByUserId = Guid.NewGuid()
        };

        _patientRepo.Setup(r => r.GetByIdAsync(patientId)).ReturnsAsync(patient);

        var mockFile = new Mock<IFormFile>();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.UploadAndProcessScanAsync(
                new MriScanUploadDTO { PatientId = patientId, File = mockFile.Object },
                userId));
    }
}
