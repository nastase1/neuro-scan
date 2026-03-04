using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeuroScan.Application.IServices;
using NeuroScan.Domain.Entities;
using NeuroScan.Domain.IRepositories;

namespace NeuroScan.Application.Services;

public class MriScanService : IMriScanService
{
    private readonly IMriScanRepository _mriScanRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAnalysisResultRepository _analysisResultRepository;
    private readonly IAiAnalysisService _aiAnalysisService;
    private readonly IOpenAiReportService _openAiReportService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<MriScanService> _logger;
    private readonly string _uploadPath;
    private readonly string _trainingDataPath;

    public MriScanService(
        IMriScanRepository mriScanRepository,
        IPatientRepository patientRepository,
        IUserRepository userRepository,
        IAnalysisResultRepository analysisResultRepository,
        IAiAnalysisService aiAnalysisService,
        IOpenAiReportService openAiReportService,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<MriScanService> logger,
        IConfiguration configuration)
    {
        _mriScanRepository = mriScanRepository;
        _patientRepository = patientRepository;
        _userRepository = userRepository;
        _analysisResultRepository = analysisResultRepository;
        _aiAnalysisService = aiAnalysisService;
        _openAiReportService = openAiReportService;
        _serviceScopeFactory = serviceScopeFactory;
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

        // Start async processing (fire-and-forget) with scan ID
        var scanId = mriScan.Id;
        _ = Task.Run(async () => await ProcessScanAsync(scanId));

        return new MriScanResponseDTO
        {
            ScanId = mriScan.Id,
            Message = "Scan uploaded successfully. Processing started.",
            Status = ScanStatus.Processing
        };
    }

    public async Task<MriScanResponseDTO> UploadSelfScanAsync(IFormFile file, string? notes, Guid userId)
    {
        // Get user details
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        // Find or create patient record for this user
        var patients = await _patientRepository.GetByUserIdAsync(userId);
        var patient = patients.FirstOrDefault();

        if (patient == null)
        {
            // Create a new patient record for this user
            patient = new Patient
            {
                Id = Guid.NewGuid(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = DateTime.UtcNow.AddYears(-30), // Default placeholder
                MedicalRecordNumber = $"SELF-{userId.ToString()[..8].ToUpper()}",
                CreatedByUserId = userId, // User creates their own patient record
                CreatedAt = DateTime.UtcNow
            };

            await _patientRepository.AddAsync(patient);
        }

        // Save file to disk
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(_uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Create MriScan record
        var mriScan = new MriScan
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            OriginalFileName = file.FileName,
            StoredFilePath = filePath,
            UploadDate = DateTime.UtcNow,
            Status = ScanStatus.Uploaded,
            CreatedAt = DateTime.UtcNow
        };

        await _mriScanRepository.AddAsync(mriScan);

        // Start async processing (fire-and-forget) with scan ID
        var scanId = mriScan.Id;
        _ = Task.Run(async () => await ProcessScanAsync(scanId));

        return new MriScanResponseDTO
        {
            ScanId = mriScan.Id,
            Message = "Scan uploaded successfully. Processing started.",
            Status = ScanStatus.Processing
        };
    }

    private async Task ProcessScanAsync(Guid scanId)
    {
        // Create a new scope to get fresh DbContext
        using var scope = _serviceScopeFactory.CreateScope();
        var mriScanRepository = scope.ServiceProvider.GetRequiredService<IMriScanRepository>();
        var patientRepository = scope.ServiceProvider.GetRequiredService<IPatientRepository>();
        var analysisResultRepository = scope.ServiceProvider.GetRequiredService<IAnalysisResultRepository>();
        var aiAnalysisService = scope.ServiceProvider.GetRequiredService<IAiAnalysisService>();
        var openAiReportService = scope.ServiceProvider.GetRequiredService<IOpenAiReportService>();

        try
        {
            // Get scan from DB
            var mriScan = await mriScanRepository.GetByIdAsync(scanId);
            if (mriScan == null)
            {
                _logger.LogError($"Scan {scanId} not found");
                return;
            }

            // Update status
            mriScan.Status = ScanStatus.Processing;
            await mriScanRepository.UpdateAsync(mriScan);

            // Step 1: Call Python AI service
            _logger.LogInformation($"Calling AI service for scan {mriScan.Id}");
            var aiResult = await aiAnalysisService.AnalyzeMriScanAsync(mriScan.StoredFilePath);

            // Step 2: Get patient details for report context
            var patient = await patientRepository.GetByIdAsync(mriScan.PatientId);
            if (patient == null)
            {
                _logger.LogError($"Patient {mriScan.PatientId} not found for scan {scanId}");
                mriScan.Status = ScanStatus.Failed;
                await mriScanRepository.UpdateAsync(mriScan);
                return;
            }

            var patientContext = new PatientContextDTO
            {
                PatientName = $"{patient.FirstName} {patient.LastName}",
                Age = CalculateAge(patient.DateOfBirth),
                ScanDate = mriScan.UploadDate
            };

            // Step 3: Generate medical report with OpenAI
            _logger.LogInformation($"Generating medical report for scan {mriScan.Id}");
            var medicalReport = await openAiReportService.GenerateMedicalReportAsync(aiResult, patientContext);

            // Step 4: Save analysis result (both models)
            var analysisResult = new AnalysisResult
            {
                Id = Guid.NewGuid(),
                MriScanId = mriScan.Id,
                // Model 1 (UNet) Results
                CsfVolume = aiResult.Model1.CsfVolume,
                GmVolume = aiResult.Model1.GmVolume,
                WmVolume = aiResult.Model1.WmVolume,
                AsymmetryIndex = aiResult.Model1.AsymmetryIndex,
                // Model 2 (SegResNet) Results
                CsfVolumeModel2 = aiResult.Model2.CsfVolume,
                GmVolumeModel2 = aiResult.Model2.GmVolume,
                WmVolumeModel2 = aiResult.Model2.WmVolume,
                AsymmetryIndexModel2 = aiResult.Model2.AsymmetryIndex,
                // Comparison Metrics
                DiceScoreCsf = aiResult.Comparison.DiceScores.Csf,
                DiceScoreGm = aiResult.Comparison.DiceScores.Gm,
                DiceScoreWm = aiResult.Comparison.DiceScores.Wm,
                DisagreementPercentage = aiResult.Comparison.DisagreementPercentage,
                RecommendedModel = aiResult.Comparison.RecommendedModel,
                ModelConfidence = aiResult.Comparison.Confidence,
                // Report
                MedicalReportText = medicalReport,
                AnalyzedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await analysisResultRepository.AddAsync(analysisResult);

            // Update scan status
            mriScan.Status = ScanStatus.Analyzed;
            await mriScanRepository.UpdateAsync(mriScan);

            _logger.LogInformation($"Scan {mriScan.Id} processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing scan {scanId}");

            // Try to update scan status to Failed
            try
            {
                var mriScan = await mriScanRepository.GetByIdAsync(scanId);
                if (mriScan != null)
                {
                    mriScan.Status = ScanStatus.Failed;
                    await mriScanRepository.UpdateAsync(mriScan);
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, $"Failed to update scan status for {scanId}");
            }
        }
    }

    public async Task<MriScanDetailDTO?> GetScanDetailsAsync(Guid scanId, Guid userId, bool isDoctor = false)
    {
        var scan = await _mriScanRepository.GetByIdAsync(scanId);
        if (scan == null) return null;

        var patient = await _patientRepository.GetByIdAsync(scan.PatientId);
        if (patient == null) return null;

        // Check access: doctors can see all, regular users only their own patients
        if (!isDoctor && patient.CreatedByUserId != userId) return null;

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
                // Model 1 (UNet)
                CsfVolume = analysisResult.CsfVolume,
                GmVolume = analysisResult.GmVolume,
                WmVolume = analysisResult.WmVolume,
                AsymmetryIndex = analysisResult.AsymmetryIndex,
                // Model 2 (SegResNet)
                CsfVolumeModel2 = analysisResult.CsfVolumeModel2,
                GmVolumeModel2 = analysisResult.GmVolumeModel2,
                WmVolumeModel2 = analysisResult.WmVolumeModel2,
                AsymmetryIndexModel2 = analysisResult.AsymmetryIndexModel2,
                // Comparison metrics
                DiceScoreCsf = analysisResult.DiceScoreCsf,
                DiceScoreGm = analysisResult.DiceScoreGm,
                DiceScoreWm = analysisResult.DiceScoreWm,
                DisagreementPercentage = analysisResult.DisagreementPercentage,
                RecommendedModel = analysisResult.RecommendedModel,
                ModelConfidence = analysisResult.ModelConfidence,
                // Report
                MedicalReportText = analysisResult.MedicalReportText,
                AnalyzedAt = analysisResult.AnalyzedAt
            } : null
        };
    }

    public async Task<IEnumerable<MriScanDetailDTO>> GetScansByPatientIdAsync(Guid patientId, Guid doctorId)
    {
        // Verify patient exists and doctor has access
        var patient = await _patientRepository.GetByIdAsync(patientId);
        if (patient == null || patient.CreatedByUserId != doctorId)
        {
            throw new UnauthorizedAccessException("Patient not found or access denied");
        }

        // Get all scans for this patient
        var scans = await _mriScanRepository.GetByPatientIdAsync(patientId);
        var scanDetails = new List<MriScanDetailDTO>();

        foreach (var scan in scans)
        {
            var analysisResult = await _analysisResultRepository.GetByMriScanIdAsync(scan.Id);

            scanDetails.Add(new MriScanDetailDTO
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
                    // Model 1 (UNet)
                    CsfVolume = analysisResult.CsfVolume,
                    GmVolume = analysisResult.GmVolume,
                    WmVolume = analysisResult.WmVolume,
                    AsymmetryIndex = analysisResult.AsymmetryIndex,
                    // Model 2 (SegResNet)
                    CsfVolumeModel2 = analysisResult.CsfVolumeModel2,
                    GmVolumeModel2 = analysisResult.GmVolumeModel2,
                    WmVolumeModel2 = analysisResult.WmVolumeModel2,
                    AsymmetryIndexModel2 = analysisResult.AsymmetryIndexModel2,
                    // Comparison metrics
                    DiceScoreCsf = analysisResult.DiceScoreCsf,
                    DiceScoreGm = analysisResult.DiceScoreGm,
                    DiceScoreWm = analysisResult.DiceScoreWm,
                    DisagreementPercentage = analysisResult.DisagreementPercentage,
                    RecommendedModel = analysisResult.RecommendedModel,
                    ModelConfidence = analysisResult.ModelConfidence,
                    // Report
                    MedicalReportText = analysisResult.MedicalReportText,
                    AnalyzedAt = analysisResult.AnalyzedAt
                } : null
            });
        }

        return scanDetails;
    }

    public async Task<IEnumerable<MriScanDetailDTO>> GetMyScansAsync(Guid userId)
    {
        // Get all patients owned by this user (self-scans & doctor-created for them)
        var patients = await _patientRepository.GetByUserIdAsync(userId);
        var scanDetails = new List<MriScanDetailDTO>();

        foreach (var patient in patients)
        {
            var scans = await _mriScanRepository.GetByPatientIdAsync(patient.Id);

            foreach (var scan in scans)
            {
                var analysisResult = await _analysisResultRepository.GetByMriScanIdAsync(scan.Id);

                scanDetails.Add(new MriScanDetailDTO
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
                        CsfVolumeModel2 = analysisResult.CsfVolumeModel2,
                        GmVolumeModel2 = analysisResult.GmVolumeModel2,
                        WmVolumeModel2 = analysisResult.WmVolumeModel2,
                        AsymmetryIndexModel2 = analysisResult.AsymmetryIndexModel2,
                        DiceScoreCsf = analysisResult.DiceScoreCsf,
                        DiceScoreGm = analysisResult.DiceScoreGm,
                        DiceScoreWm = analysisResult.DiceScoreWm,
                        DisagreementPercentage = analysisResult.DisagreementPercentage,
                        RecommendedModel = analysisResult.RecommendedModel,
                        ModelConfidence = analysisResult.ModelConfidence,
                        MedicalReportText = analysisResult.MedicalReportText,
                        AnalyzedAt = analysisResult.AnalyzedAt
                    } : null
                });
            }
        }

        // Newest first
        return scanDetails.OrderByDescending(s => s.UploadDate);
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
