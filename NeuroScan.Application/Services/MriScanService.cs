using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeuroScan.Application.IServices;
using NeuroScan.Domain.Entities;
using NeuroScan.Domain.IRepositories;

namespace NeuroScan.Application.Services;

public class MriScanService : IMriScanService
{
    private readonly IMriScanRepository _mriScanRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IAnalysisResultRepository _analysisResultRepository;
    private readonly IAiAnalysisService _aiAnalysisService;
    private readonly IOpenAiReportService _openAiReportService;
    private readonly ILogger<MriScanService> _logger;
    private readonly string _uploadPath;
    private readonly string _trainingDataPath;

    public MriScanService(
        IMriScanRepository mriScanRepository,
        IPatientRepository patientRepository,
        IAnalysisResultRepository analysisResultRepository,
        IAiAnalysisService aiAnalysisService,
        IOpenAiReportService openAiReportService,
        ILogger<MriScanService> logger,
        IConfiguration configuration)
    {
        _mriScanRepository = mriScanRepository;
        _patientRepository = patientRepository;
        _analysisResultRepository = analysisResultRepository;
        _aiAnalysisService = aiAnalysisService;
        _openAiReportService = openAiReportService;
        _logger = logger;
        _uploadPath = configuration["Storage:UploadPath"] ?? "uploads/scans";
        _trainingDataPath = configuration["Storage:TrainingDataPath"] ?? "uploads/training-data";

        // Ensure directories exist
        Directory.CreateDirectory(_uploadPath);
        Directory.CreateDirectory(_trainingDataPath);
    }

    public async Task<MriScanResponseDTO> UploadAndProcessScanAsync(MriScanUploadDTO uploadDto, Guid userId)
    {
        // Verify patient exists and user has access
        var patient = await _patientRepository.GetByIdAsync(uploadDto.PatientId);
        if (patient == null || patient.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("Patient not found or access denied");
        }

        // Save file to disk
        var fileName = $"{Guid.NewGuid()}_{uploadDto.File.FileName}";
        var filePath = Path.Combine(_uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await uploadDto.File.CopyToAsync(stream);
        }

        // Create MriScan record
        var mriScan = new MriScan
        {
            Id = Guid.NewGuid(),
            PatientId = uploadDto.PatientId,
            OriginalFileName = uploadDto.File.FileName,
            StoredFilePath = filePath,
            UploadDate = DateTime.UtcNow,
            Status = ScanStatus.Uploaded,
            CreatedAt = DateTime.UtcNow
        };

        await _mriScanRepository.AddAsync(mriScan);

        // Start async processing (fire-and-forget)
        _ = Task.Run(async () => await ProcessScanAsync(mriScan));

        return new MriScanResponseDTO
        {
            ScanId = mriScan.Id,
            Message = "Scan uploaded successfully. Processing started.",
            Status = ScanStatus.Processing
        };
    }

    private async Task ProcessScanAsync(MriScan mriScan)
    {
        try
        {
            // Update status
            mriScan.Status = ScanStatus.Processing;
            await _mriScanRepository.UpdateAsync(mriScan);

            // Step 1: Call Python AI service
            _logger.LogInformation($"Calling AI service for scan {mriScan.Id}");
            var aiResult = await _aiAnalysisService.AnalyzeMriScanAsync(mriScan.StoredFilePath);

            // Step 2: Get patient details for report context
            var patient = await _patientRepository.GetByIdAsync(mriScan.PatientId);
            var patientContext = new PatientContextDTO
            {
                PatientName = $"{patient!.FirstName} {patient.LastName}",
                Age = CalculateAge(patient.DateOfBirth),
                ScanDate = mriScan.UploadDate
            };

            // Step 3: Generate medical report with OpenAI
            _logger.LogInformation($"Generating medical report for scan {mriScan.Id}");
            var medicalReport = await _openAiReportService.GenerateMedicalReportAsync(aiResult, patientContext);

            // Step 4: Save analysis result
            var analysisResult = new AnalysisResult
            {
                Id = Guid.NewGuid(),
                MriScanId = mriScan.Id,
                CsfVolume = aiResult.CsfVolume,
                GmVolume = aiResult.GmVolume,
                WmVolume = aiResult.WmVolume,
                AsymmetryIndex = aiResult.AsymmetryIndex,
                MedicalReportText = medicalReport,
                AnalyzedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _analysisResultRepository.AddAsync(analysisResult);

            // Update scan status
            mriScan.Status = ScanStatus.Analyzed;
            await _mriScanRepository.UpdateAsync(mriScan);

            _logger.LogInformation($"Scan {mriScan.Id} processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing scan {mriScan.Id}");
            mriScan.Status = ScanStatus.Failed;
            await _mriScanRepository.UpdateAsync(mriScan);
        }
    }

    public async Task<MriScanDetailDTO?> GetScanDetailsAsync(Guid scanId, Guid userId)
    {
        var scan = await _mriScanRepository.GetByIdAsync(scanId);
        if (scan == null) return null;

        var patient = await _patientRepository.GetByIdAsync(scan.PatientId);
        if (patient == null || patient.CreatedByUserId != userId) return null;

        var analysisResult = await _analysisResultRepository.GetByMriScanIdAsync(scanId);

        return new MriScanDetailDTO
        {
            Id = scan.Id,
            OriginalFileName = scan.OriginalFileName,
            UploadDate = scan.UploadDate,
            Status = scan.Status,
            Patient = new PatientBasicDTO
            {
                Id = patient.Id,
                FullName = $"{patient.FirstName} {patient.LastName}",
                MedicalRecordNumber = patient.MedicalRecordNumber
            },
            AnalysisResult = analysisResult != null ? new AnalysisResultDTO
            {
                CsfVolume = analysisResult.CsfVolume,
                GmVolume = analysisResult.GmVolume,
                WmVolume = analysisResult.WmVolume,
                AsymmetryIndex = analysisResult.AsymmetryIndex,
                MedicalReportText = analysisResult.MedicalReportText,
                AnalyzedAt = analysisResult.AnalyzedAt
            } : null
        };
    }

    public async Task SubmitCorrectedMaskAsync(Guid scanId, IFormFile correctedMask, Guid doctorId)
    {
        var scan = await _mriScanRepository.GetByIdAsync(scanId);
        if (scan == null)
        {
            throw new ArgumentException("Scan not found");
        }

        // Save corrected mask to training data folder
        var fileName = $"corrected_{scanId}_{correctedMask.FileName}";
        var filePath = Path.Combine(_trainingDataPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await correctedMask.CopyToAsync(stream);
        }

        // Update scan record
        scan.CorrectedMaskPath = filePath;
        scan.ReviewedByDoctorId = doctorId;
        scan.ReviewedAt = DateTime.UtcNow;
        scan.Status = ScanStatus.ReviewedByDoctor;
        scan.UpdatedAt = DateTime.UtcNow;

        await _mriScanRepository.UpdateAsync(scan);

        _logger.LogInformation($"Doctor {doctorId} submitted corrected mask for scan {scanId}");
    }

    public async Task<IEnumerable<MriScanSummaryDTO>> GetPendingReviewScansAsync()
    {
        var scans = await _mriScanRepository.GetByStatusAsync(ScanStatus.Analyzed);
        var summaries = new List<MriScanSummaryDTO>();

        foreach (var scan in scans)
        {
            var patient = await _patientRepository.GetByIdAsync(scan.PatientId);
            summaries.Add(new MriScanSummaryDTO
            {
                Id = scan.Id,
                PatientName = $"{patient!.FirstName} {patient.LastName}",
                UploadDate = scan.UploadDate,
                Status = scan.Status
            });
        }

        return summaries;
    }

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }
}
