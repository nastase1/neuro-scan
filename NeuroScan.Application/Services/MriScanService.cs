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

    public async Task<MriScanResponseDTO> UploadAndProcessTumorScanAsync(MriScanUploadTumorDTO uploadDto, Guid userId)
    {
        var patient = await _patientRepository.GetByIdAsync(uploadDto.PatientId);
        if (patient == null || patient.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("Patient not found or access denied");
        }

        // Save all 4 files to disk
        var t1Name = $"{Guid.NewGuid()}_{uploadDto.T1File.FileName}";
        var t1Path = Path.Combine(_uploadPath, t1Name);
        using (var stream = new FileStream(t1Path, FileMode.Create))
            await uploadDto.T1File.CopyToAsync(stream);

        var t1ceName = $"{Guid.NewGuid()}_{uploadDto.T1ceFile.FileName}";
        var t1cePath = Path.Combine(_uploadPath, t1ceName);
        using (var stream = new FileStream(t1cePath, FileMode.Create))
            await uploadDto.T1ceFile.CopyToAsync(stream);

        var t2Name = $"{Guid.NewGuid()}_{uploadDto.T2File.FileName}";
        var t2Path = Path.Combine(_uploadPath, t2Name);
        using (var stream = new FileStream(t2Path, FileMode.Create))
            await uploadDto.T2File.CopyToAsync(stream);

        var flairName = $"{Guid.NewGuid()}_{uploadDto.FlairFile.FileName}";
        var flairPath = Path.Combine(_uploadPath, flairName);
        using (var stream = new FileStream(flairPath, FileMode.Create))
            await uploadDto.FlairFile.CopyToAsync(stream);

        var mriScan = new MriScan
        {
            Id = Guid.NewGuid(),
            PatientId = uploadDto.PatientId,
            OriginalFileName = uploadDto.T1File.FileName,
            StoredFilePath = t1Path,
            StoredFilePathT1ce = t1cePath,
            StoredFilePathT2 = t2Path,
            StoredFilePathFlair = flairPath,
            ScanType = ScanType.TumorMultiChannel,
            UploadDate = DateTime.UtcNow,
            Status = ScanStatus.Uploaded,
            CreatedAt = DateTime.UtcNow
        };

        await _mriScanRepository.AddAsync(mriScan);

        var scanId = mriScan.Id;
        _ = Task.Run(async () => await ProcessTumorScanAsync(scanId));

        return new MriScanResponseDTO
        {
            ScanId = mriScan.Id,
            Message = "Tumor scan uploaded successfully. Processing started.",
            Status = ScanStatus.Processing
        };
    }

    public async Task<MriScanResponseDTO> UploadSelfTumorScanAsync(IFormFile t1, IFormFile t1ce, IFormFile t2, IFormFile flair, string? notes, Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        var patients = await _patientRepository.GetByUserIdAsync(userId);
        var patient = patients.FirstOrDefault();

        if (patient == null)
        {
            patient = new Patient
            {
                Id = Guid.NewGuid(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                MedicalRecordNumber = $"SELF-{userId.ToString()[..8].ToUpper()}",
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _patientRepository.AddAsync(patient);
        }

        // Save all 4 files
        var t1Name = $"{Guid.NewGuid()}_{t1.FileName}";
        var t1Path = Path.Combine(_uploadPath, t1Name);
        using (var stream = new FileStream(t1Path, FileMode.Create))
            await t1.CopyToAsync(stream);

        var t1ceName = $"{Guid.NewGuid()}_{t1ce.FileName}";
        var t1cePath = Path.Combine(_uploadPath, t1ceName);
        using (var stream = new FileStream(t1cePath, FileMode.Create))
            await t1ce.CopyToAsync(stream);

        var t2Name = $"{Guid.NewGuid()}_{t2.FileName}";
        var t2Path = Path.Combine(_uploadPath, t2Name);
        using (var stream = new FileStream(t2Path, FileMode.Create))
            await t2.CopyToAsync(stream);

        var flairName = $"{Guid.NewGuid()}_{flair.FileName}";
        var flairPath = Path.Combine(_uploadPath, flairName);
        using (var stream = new FileStream(flairPath, FileMode.Create))
            await flair.CopyToAsync(stream);

        var mriScan = new MriScan
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            OriginalFileName = t1.FileName,
            StoredFilePath = t1Path,
            StoredFilePathT1ce = t1cePath,
            StoredFilePathT2 = t2Path,
            StoredFilePathFlair = flairPath,
            ScanType = ScanType.TumorMultiChannel,
            UploadDate = DateTime.UtcNow,
            Status = ScanStatus.Uploaded,
            CreatedAt = DateTime.UtcNow
        };

        await _mriScanRepository.AddAsync(mriScan);

        var scanId = mriScan.Id;
        _ = Task.Run(async () => await ProcessTumorScanAsync(scanId));

        return new MriScanResponseDTO
        {
            ScanId = mriScan.Id,
            Message = "Tumor scan uploaded successfully. Processing started.",
            Status = ScanStatus.Processing
        };
    }

    private async Task ProcessTumorScanAsync(Guid scanId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var mriScanRepository = scope.ServiceProvider.GetRequiredService<IMriScanRepository>();
        var patientRepository = scope.ServiceProvider.GetRequiredService<IPatientRepository>();
        var analysisResultRepository = scope.ServiceProvider.GetRequiredService<IAnalysisResultRepository>();
        var aiAnalysisService = scope.ServiceProvider.GetRequiredService<IAiAnalysisService>();
        var openAiReportService = scope.ServiceProvider.GetRequiredService<IOpenAiReportService>();

        try
        {
            var mriScan = await mriScanRepository.GetByIdAsync(scanId);
            if (mriScan == null)
            {
                _logger.LogError($"Tumor scan {scanId} not found");
                return;
            }

            mriScan.Status = ScanStatus.Processing;
            await mriScanRepository.UpdateAsync(mriScan);

            // Call tumor endpoint with all 4 files
            _logger.LogInformation($"Calling AI tumor service for scan {mriScan.Id}");
            var aiResult = await aiAnalysisService.AnalyzeTumorScanAsync(
                mriScan.StoredFilePath,
                mriScan.StoredFilePathT1ce!,
                mriScan.StoredFilePathT2!,
                mriScan.StoredFilePathFlair!);

            var accurateRiskLevel = GetEpilepsyRiskLevel(aiResult.Epilepsy.RiskScore);
            aiResult.Epilepsy.RiskLevel = accurateRiskLevel;

            // Get patient for report
            var patient = await patientRepository.GetByIdAsync(mriScan.PatientId);
            if (patient == null)
            {
                _logger.LogError($"Patient {mriScan.PatientId} not found for tumor scan {scanId}");
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

            _logger.LogInformation($"Generating medical report for tumor scan {mriScan.Id}");
            var medicalReport = await openAiReportService.GenerateMedicalReportAsync(aiResult, patientContext);

            // Save segmentation slices
            string? segImageDir = null;
            int segSliceCount = 0;
            if (aiResult.SegmentationSlices.Count > 0)
            {
                try
                {
                    var imgDir = Path.Combine(_uploadPath, "segmentation-images");
                    Directory.CreateDirectory(imgDir);
                    segImageDir = Path.Combine(imgDir, mriScan.Id.ToString());
                    Directory.CreateDirectory(segImageDir);
                    for (int i = 0; i < aiResult.SegmentationSlices.Count; i++)
                    {
                        var imgBytes = Convert.FromBase64String(aiResult.SegmentationSlices[i]);
                        await File.WriteAllBytesAsync(Path.Combine(segImageDir, $"{i}.png"), imgBytes);
                    }
                    segSliceCount = aiResult.SegmentationSlices.Count;
                }
                catch (Exception imgEx)
                {
                    _logger.LogWarning(imgEx, $"Failed to save segmentation slices for tumor scan {mriScan.Id}");
                }
            }

            // Save tumor overlay slices
            string? tumorOverlayDir = null;
            int tumorOverlayCount = 0;
            if (aiResult.TumorOverlaySlices.Count > 0)
            {
                try
                {
                    var overlayRoot = Path.Combine(_uploadPath, "tumor-overlay-images");
                    Directory.CreateDirectory(overlayRoot);
                    tumorOverlayDir = Path.Combine(overlayRoot, mriScan.Id.ToString());
                    Directory.CreateDirectory(tumorOverlayDir);
                    for (int i = 0; i < aiResult.TumorOverlaySlices.Count; i++)
                    {
                        var imgBytes = Convert.FromBase64String(aiResult.TumorOverlaySlices[i]);
                        await File.WriteAllBytesAsync(Path.Combine(tumorOverlayDir, $"{i}.png"), imgBytes);
                    }
                    tumorOverlayCount = aiResult.TumorOverlaySlices.Count;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to save tumor overlay slices for tumor scan {mriScan.Id}");
                }
            }

            // Save analysis result
            var analysisResult = new AnalysisResult
            {
                Id = Guid.NewGuid(),
                MriScanId = mriScan.Id,
                CsfVolume = aiResult.Segresnet.CsfVolume,
                GmVolume = aiResult.Segresnet.GmVolume,
                WmVolume = aiResult.Segresnet.WmVolume,
                AsymmetryIndex = aiResult.Segresnet.AsymmetryIndex,
                EpilepsyRiskScore = aiResult.Epilepsy.RiskScore,
                EpilepsyRiskLevel = accurateRiskLevel,
                TumorDetected = aiResult.Tumor.TumorDetected,
                TumorVolume = aiResult.Tumor.TumorVolume,
                TumorSurfaceArea = aiResult.Tumor.TumorSurfaceArea,
                CortexThicknessAvg = aiResult.CortexThickness.AvgThickness,
                CortexThicknessMin = aiResult.CortexThickness.MinThickness,
                CortexThicknessMax = aiResult.CortexThickness.MaxThickness,
                WmDensityScore = aiResult.WhiteMatterDensity.DensityScore,
                WmMeanIntensity = aiResult.WhiteMatterDensity.MeanIntensity,
                WmCoefficientOfVariation = aiResult.WhiteMatterDensity.CoefficientOfVariation,
                SegmentationImagePath = segImageDir,
                SegmentationSliceCount = segSliceCount,
                TumorOverlayImagePath = tumorOverlayDir,
                TumorOverlaySliceCount = tumorOverlayCount,
                MedicalReportText = medicalReport,
                AnalyzedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await analysisResultRepository.AddAsync(analysisResult);

            mriScan.Status = ScanStatus.Analyzed;
            await mriScanRepository.UpdateAsync(mriScan);

            _logger.LogInformation($"Tumor scan {mriScan.Id} processed successfully — Tumor detected: {aiResult.Tumor.TumorDetected}");

            // Email results
            if (!string.IsNullOrWhiteSpace(patient.Email))
            {
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                try
                {
                    var emailData = new ScanResultEmailData
                    {
                        ScanDate = mriScan.UploadDate,
                        MedicalReport = medicalReport,
                        CsfVolume = aiResult.Segresnet.CsfVolume,
                        GmVolume = aiResult.Segresnet.GmVolume,
                        WmVolume = aiResult.Segresnet.WmVolume,
                        AsymmetryIndex = aiResult.Segresnet.AsymmetryIndex,
                        EpilepsyRiskScore = aiResult.Epilepsy.RiskScore,
                        EpilepsyRiskLevel = accurateRiskLevel
                    };
                    await emailService.SendScanResultsEmailAsync(
                        patient.Email,
                        $"{patient.FirstName} {patient.LastName}",
                        emailData);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, $"Failed to send tumor scan results email to patient {patient.Id}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing tumor scan {scanId}");
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
                _logger.LogError(updateEx, $"Failed to update tumor scan status for {scanId}");
            }
        }
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

            // Step 1: Call Python AI service (SegResNet)
            _logger.LogInformation($"Calling AI service for scan {mriScan.Id}");
            var aiResult = await aiAnalysisService.AnalyzeMriScanAsync(mriScan.StoredFilePath);

            // Normalize risk level from score to ensure consistent, accurate classification.
            // 0-39: Low, 40-69: Moderate, 70-100: High
            var accurateRiskLevel = GetEpilepsyRiskLevel(aiResult.Epilepsy.RiskScore);
            aiResult.Epilepsy.RiskLevel = accurateRiskLevel;

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

            // Step 4: Save segmentation slices to disk
            string? segImageDir = null;
            int segSliceCount = 0;
            if (aiResult.SegmentationSlices.Count > 0)
            {
                try
                {
                    var imgDir = Path.Combine(_uploadPath, "segmentation-images");
                    Directory.CreateDirectory(imgDir);
                    segImageDir = Path.Combine(imgDir, mriScan.Id.ToString());
                    Directory.CreateDirectory(segImageDir);
                    for (int i = 0; i < aiResult.SegmentationSlices.Count; i++)
                    {
                        var imgBytes = Convert.FromBase64String(aiResult.SegmentationSlices[i]);
                        await File.WriteAllBytesAsync(Path.Combine(segImageDir, $"{i}.png"), imgBytes);
                    }
                    segSliceCount = aiResult.SegmentationSlices.Count;
                }
                catch (Exception imgEx)
                {
                    _logger.LogWarning(imgEx, $"Failed to save segmentation slices for scan {mriScan.Id}");
                    segImageDir = null;
                    segSliceCount = 0;
                }
            }

            // Step 4b: Save tumor overlay slices to disk
            string? tumorOverlayDir = null;
            int tumorOverlayCount = 0;
            if (aiResult.TumorOverlaySlices.Count > 0)
            {
                try
                {
                    var overlayRoot = Path.Combine(_uploadPath, "tumor-overlay-images");
                    Directory.CreateDirectory(overlayRoot);
                    tumorOverlayDir = Path.Combine(overlayRoot, mriScan.Id.ToString());
                    Directory.CreateDirectory(tumorOverlayDir);
                    for (int i = 0; i < aiResult.TumorOverlaySlices.Count; i++)
                    {
                        var imgBytes = Convert.FromBase64String(aiResult.TumorOverlaySlices[i]);
                        await File.WriteAllBytesAsync(Path.Combine(tumorOverlayDir, $"{i}.png"), imgBytes);
                    }
                    tumorOverlayCount = aiResult.TumorOverlaySlices.Count;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to save tumor overlay slices for scan {mriScan.Id}");
                    tumorOverlayDir = null;
                    tumorOverlayCount = 0;
                }
            }

            // Step 5: Save analysis result
            var analysisResult = new AnalysisResult
            {
                Id = Guid.NewGuid(),
                MriScanId = mriScan.Id,
                // SegResNet volumetrics
                CsfVolume = aiResult.Segresnet.CsfVolume,
                GmVolume = aiResult.Segresnet.GmVolume,
                WmVolume = aiResult.Segresnet.WmVolume,
                AsymmetryIndex = aiResult.Segresnet.AsymmetryIndex,
                // Epilepsy risk
                EpilepsyRiskScore = aiResult.Epilepsy.RiskScore,
                EpilepsyRiskLevel = accurateRiskLevel,
                // Tumor detection
                TumorDetected = aiResult.Tumor.TumorDetected,
                TumorVolume = aiResult.Tumor.TumorVolume,
                TumorSurfaceArea = aiResult.Tumor.TumorSurfaceArea,
                // Cortex thickness
                CortexThicknessAvg = aiResult.CortexThickness.AvgThickness,
                CortexThicknessMin = aiResult.CortexThickness.MinThickness,
                CortexThicknessMax = aiResult.CortexThickness.MaxThickness,
                // White matter density
                WmDensityScore = aiResult.WhiteMatterDensity.DensityScore,
                WmMeanIntensity = aiResult.WhiteMatterDensity.MeanIntensity,
                WmCoefficientOfVariation = aiResult.WhiteMatterDensity.CoefficientOfVariation,
                // Segmentation slices
                SegmentationImagePath = segImageDir,
                SegmentationSliceCount = segSliceCount,
                // Tumor overlay slices
                TumorOverlayImagePath = tumorOverlayDir,
                TumorOverlaySliceCount = tumorOverlayCount,
                // Report
                MedicalReportText = medicalReport,
                AnalyzedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await analysisResultRepository.AddAsync(analysisResult);

            // Update scan status
            mriScan.Status = ScanStatus.Analyzed;
            await mriScanRepository.UpdateAsync(mriScan);

            _logger.LogInformation($"Scan {mriScan.Id} processed successfully — Epilepsy risk: {accurateRiskLevel}");

            // Step 6: Email results to patient (if they have an email address)
            if (!string.IsNullOrWhiteSpace(patient.Email))
            {
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                try
                {
                    var emailData = new ScanResultEmailData
                    {
                        ScanDate = mriScan.UploadDate,
                        MedicalReport = medicalReport,
                        CsfVolume = aiResult.Segresnet.CsfVolume,
                        GmVolume = aiResult.Segresnet.GmVolume,
                        WmVolume = aiResult.Segresnet.WmVolume,
                        AsymmetryIndex = aiResult.Segresnet.AsymmetryIndex,
                        EpilepsyRiskScore = aiResult.Epilepsy.RiskScore,
                        EpilepsyRiskLevel = accurateRiskLevel
                    };
                    await emailService.SendScanResultsEmailAsync(
                        patient.Email,
                        $"{patient.FirstName} {patient.LastName}",
                        emailData);
                    _logger.LogInformation($"Scan results email sent to patient {patient.Id}");
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, $"Failed to send scan results email to patient {patient.Id}");
                }
            }
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

        // Check access:
        // - doctors can see scans for patients they created
        // - standard users can only see scans for their linked patient record
        if (isDoctor)
        {
            if (patient.CreatedByUserId != userId) return null;
        }
        else
        {
            if (patient.UserId != userId) return null;
        }

        var analysisResult = await _analysisResultRepository.GetByMriScanIdAsync(scanId);

        // Legacy repair: older analyses saved segmentation slices with inconsistent indexing/geometry.
        // Regenerate once to align review-page Original vs AI slices.
        if (analysisResult != null)
        {
            await EnsureSegmentationSlicesAlignedAsync(scan, analysisResult);
            analysisResult = await _analysisResultRepository.GetByMriScanIdAsync(scanId);
        }

        return new MriScanDetailDTO
        {
            Id = scan.Id,
            OriginalFileName = scan.OriginalFileName,
            StoredFilePath = scan.StoredFilePath,
            StoredFilePathT1ce = scan.StoredFilePathT1ce,
            StoredFilePathT2 = scan.StoredFilePathT2,
            StoredFilePathFlair = scan.StoredFilePathFlair,
            ScanType = scan.ScanType,
            UploadDate = scan.UploadDate,
            Status = scan.Status,
            DoctorClinicalNotes = scan.DoctorClinicalNotes,
            Patient = new PatientBasicDTO
            {
                Id = patient.Id,
                FullName = $"{patient.FirstName} {patient.LastName}",
                MedicalRecordNumber = patient.MedicalRecordNumber
            },
            AnalysisResult = MapAnalysisResultDTO(analysisResult)
        };
    }

    private async Task EnsureSegmentationSlicesAlignedAsync(MriScan scan, AnalysisResult analysisResult)
    {
        // Current pipeline always produces full ROI depth (96) slices.
        // If count is lower, this scan likely used older slice export logic.
        if (analysisResult.SegmentationSliceCount >= 96 &&
            !string.IsNullOrWhiteSpace(analysisResult.SegmentationImagePath) &&
            Directory.Exists(analysisResult.SegmentationImagePath))
        {
            return;
        }

        try
        {
            var aiResult = await _aiAnalysisService.AnalyzeMriScanAsync(scan.StoredFilePath);
            if (aiResult.SegmentationSlices.Count == 0)
            {
                return;
            }

            var imgDir = Path.Combine(_uploadPath, "segmentation-images");
            Directory.CreateDirectory(imgDir);
            var segDir = Path.Combine(imgDir, scan.Id.ToString());

            if (Directory.Exists(segDir))
            {
                Directory.Delete(segDir, recursive: true);
            }

            Directory.CreateDirectory(segDir);
            for (int i = 0; i < aiResult.SegmentationSlices.Count; i++)
            {
                var imgBytes = Convert.FromBase64String(aiResult.SegmentationSlices[i]);
                await File.WriteAllBytesAsync(Path.Combine(segDir, $"{i}.png"), imgBytes);
            }

            analysisResult.SegmentationImagePath = segDir;
            analysisResult.SegmentationSliceCount = aiResult.SegmentationSlices.Count;

            // Also regenerate tumor overlays if available
            if (aiResult.TumorOverlaySlices.Count > 0)
            {
                var overlayRoot = Path.Combine(_uploadPath, "tumor-overlay-images");
                Directory.CreateDirectory(overlayRoot);
                var overlayDir = Path.Combine(overlayRoot, scan.Id.ToString());
                if (Directory.Exists(overlayDir)) Directory.Delete(overlayDir, recursive: true);
                Directory.CreateDirectory(overlayDir);
                for (int i = 0; i < aiResult.TumorOverlaySlices.Count; i++)
                {
                    var overlayBytes = Convert.FromBase64String(aiResult.TumorOverlaySlices[i]);
                    await File.WriteAllBytesAsync(Path.Combine(overlayDir, $"{i}.png"), overlayBytes);
                }
                analysisResult.TumorOverlayImagePath = overlayDir;
                analysisResult.TumorOverlaySliceCount = aiResult.TumorOverlaySlices.Count;
            }

            analysisResult.UpdatedAt = DateTime.UtcNow;
            await _analysisResultRepository.UpdateAsync(analysisResult);

            _logger.LogInformation($"Regenerated aligned segmentation slices for legacy scan {scan.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to regenerate segmentation slices for scan {scan.Id}");
        }
    }

    public async Task<IEnumerable<MriScanDetailDTO>> GetScansByPatientIdAsync(Guid patientId, Guid requesterId, bool isDoctor = false)
    {
        // Verify patient exists and requester has access
        var patient = await _patientRepository.GetByIdAsync(patientId);
        if (patient == null)
        {
            throw new UnauthorizedAccessException("Patient not found or access denied");
        }

        var hasAccess = isDoctor
            ? patient.CreatedByUserId == requesterId
            : patient.UserId == requesterId;

        if (!hasAccess)
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
                StoredFilePath = scan.StoredFilePath,
                UploadDate = scan.UploadDate,
                Status = scan.Status,
                DoctorClinicalNotes = scan.DoctorClinicalNotes,
                Patient = new PatientBasicDTO
                {
                    Id = patient.Id,
                    FullName = $"{patient.FirstName} {patient.LastName}",
                    MedicalRecordNumber = patient.MedicalRecordNumber
                },
                AnalysisResult = MapAnalysisResultDTO(analysisResult)
            });
        }

        return scanDetails;
    }

    public async Task<IEnumerable<MriScanDetailDTO>> GetMyScansAsync(Guid userId)
    {
        // Get the patient record linked to this user account.
        var patient = await _patientRepository.GetByPatientUserIdAsync(userId);
        var scanDetails = new List<MriScanDetailDTO>();

        if (patient != null)
        {
            var scans = await _mriScanRepository.GetByPatientIdAsync(patient.Id);

            foreach (var scan in scans)
            {
                var analysisResult = await _analysisResultRepository.GetByMriScanIdAsync(scan.Id);

                scanDetails.Add(new MriScanDetailDTO
                {
                    Id = scan.Id,
                    OriginalFileName = scan.OriginalFileName,
                    StoredFilePath = scan.StoredFilePath,
                    UploadDate = scan.UploadDate,
                    Status = scan.Status,
                    DoctorClinicalNotes = scan.DoctorClinicalNotes,
                    Patient = new PatientBasicDTO
                    {
                        Id = patient.Id,
                        FullName = $"{patient.FirstName} {patient.LastName}",
                        MedicalRecordNumber = patient.MedicalRecordNumber
                    },
                    AnalysisResult = MapAnalysisResultDTO(analysisResult)
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

    public async Task<int> GetRawSliceCountAsync(Guid scanId, Guid doctorId)
    {
        var scan = await _mriScanRepository.GetByIdAsync(scanId);
        if (scan == null) return 0;

        var analysisResult = await _analysisResultRepository.GetByMriScanIdAsync(scanId);
        var expectedSliceCount = analysisResult?.SegmentationSliceCount ?? 0;

        var rawDir = Path.Combine(_uploadPath, "raw-slices", scanId.ToString());

        // Serve from cache if already generated
        if (Directory.Exists(rawDir))
        {
            var cachedCount = Directory.GetFiles(rawDir, "*.png").Length;
            if (expectedSliceCount == 0 || cachedCount == expectedSliceCount)
            {
                return cachedCount;
            }

            // Cache is stale (generated with older pipeline); regenerate.
            Directory.Delete(rawDir, recursive: true);
        }

        // Generate: send the stored .nii to Python, save slices to disk
        if (!File.Exists(scan.StoredFilePath)) return 0;

        var slices = await _aiAnalysisService.GetRawSlicesAsync(scan.StoredFilePath);
        if (slices.Count == 0) return 0;

        Directory.CreateDirectory(rawDir);
        for (int i = 0; i < slices.Count; i++)
        {
            var bytes = Convert.FromBase64String(slices[i]);
            await File.WriteAllBytesAsync(Path.Combine(rawDir, $"{i}.png"), bytes);
        }

        return slices.Count;
    }

    public async Task<byte[]?> GetRawSliceAsync(Guid scanId, int sliceIndex, Guid doctorId)
    {
        var rawDir = Path.Combine(_uploadPath, "raw-slices", scanId.ToString());

        // Generate if not cached
        if (!Directory.Exists(rawDir))
        {
            var count = await GetRawSliceCountAsync(scanId, doctorId);
            if (count == 0) return null;
        }

        var slicePath = Path.Combine(rawDir, $"{sliceIndex}.png");
        if (!File.Exists(slicePath)) return null;

        return await File.ReadAllBytesAsync(slicePath);
    }

    public async Task SubmitReviewAsync(Guid scanId, Guid doctorId, bool approved, string notes)
    {
        var scan = await _mriScanRepository.GetByIdAsync(scanId);
        if (scan == null) throw new ArgumentException("Scan not found");

        var analysisResult = await _analysisResultRepository.GetByMriScanIdAsync(scanId);
        if (analysisResult == null) throw new InvalidOperationException("No analysis result for this scan");

        analysisResult.DoctorApproved = approved;
        analysisResult.DoctorReviewNotes = notes;
        analysisResult.UpdatedAt = DateTime.UtcNow;
        await _analysisResultRepository.UpdateAsync(analysisResult);

        scan.DoctorClinicalNotes = notes;
        scan.ReviewedByDoctorId = doctorId;
        scan.ReviewedAt = DateTime.UtcNow;
        scan.Status = ScanStatus.ReviewedByDoctor;
        scan.UpdatedAt = DateTime.UtcNow;
        await _mriScanRepository.UpdateAsync(scan);

        _logger.LogInformation($"Doctor {doctorId} submitted review for scan {scanId} — approved: {approved}");
    }

    public async Task SaveCorrectedSliceAsync(Guid scanId, int sliceIndex, string base64Png, Guid doctorId)
    {
        var scan = await _mriScanRepository.GetByIdAsync(scanId);
        if (scan == null) throw new ArgumentException("Scan not found");

        var corrDir = Path.Combine(_uploadPath, "corrected-slices", scanId.ToString());
        Directory.CreateDirectory(corrDir);

        var imgBytes = Convert.FromBase64String(base64Png);
        await File.WriteAllBytesAsync(Path.Combine(corrDir, $"{sliceIndex}.png"), imgBytes);

        _logger.LogInformation($"Doctor {doctorId} saved corrected slice {sliceIndex} for scan {scanId}");
    }

    public async Task<byte[]?> GetCorrectedSliceAsync(Guid scanId, int sliceIndex, Guid doctorId)
    {
        var corrPath = Path.Combine(_uploadPath, "corrected-slices", scanId.ToString(), $"{sliceIndex}.png");
        if (!File.Exists(corrPath)) return null;
        return await File.ReadAllBytesAsync(corrPath);
    }

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }

    private static string GetEpilepsyRiskLevel(double riskScore)
    {
        return riskScore switch
        {
            >= 70 => "High",
            >= 40 => "Moderate",
            _ => "Low"
        };
    }

    private static AnalysisResultDTO? MapAnalysisResultDTO(AnalysisResult? ar)
    {
        if (ar == null) return null;
        return new AnalysisResultDTO
        {
            CsfVolume = ar.CsfVolume,
            GmVolume = ar.GmVolume,
            WmVolume = ar.WmVolume,
            AsymmetryIndex = ar.AsymmetryIndex,
            EpilepsyRiskScore = ar.EpilepsyRiskScore,
            EpilepsyRiskLevel = ar.EpilepsyRiskLevel,
            TumorDetected = ar.TumorDetected,
            TumorVolume = ar.TumorVolume,
            TumorSurfaceArea = ar.TumorSurfaceArea,
            CortexThicknessAvg = ar.CortexThicknessAvg,
            CortexThicknessMin = ar.CortexThicknessMin,
            CortexThicknessMax = ar.CortexThicknessMax,
            WmDensityScore = ar.WmDensityScore,
            WmMeanIntensity = ar.WmMeanIntensity,
            WmCoefficientOfVariation = ar.WmCoefficientOfVariation,
            SegmentationImagePath = ar.SegmentationImagePath,
            SegmentationSliceCount = ar.SegmentationSliceCount,
            TumorOverlaySliceCount = ar.TumorOverlaySliceCount,
            TumorOverlayImagePath = ar.TumorOverlayImagePath,
            MedicalReportText = ar.MedicalReportText,
            AnalyzedAt = ar.AnalyzedAt
        };
    }

    public async Task<PatientEvolutionDTO> GetPatientEvolutionAsync(Guid patientId, Guid requesterId, bool isDoctor = false)
    {
        var patient = await _patientRepository.GetByIdAsync(patientId);
        if (patient == null)
            throw new UnauthorizedAccessException("Patient not found or access denied");

        var hasAccess = isDoctor
            ? patient.CreatedByUserId == requesterId
            : patient.UserId == requesterId;
        if (!hasAccess)
            throw new UnauthorizedAccessException("Patient not found or access denied");

        var scans = await _mriScanRepository.GetByPatientIdAsync(patientId);
        var dataPoints = new List<EvolutionDataPointDTO>();

        foreach (var scan in scans.OrderBy(s => s.UploadDate))
        {
            var ar = await _analysisResultRepository.GetByMriScanIdAsync(scan.Id);
            if (ar == null) continue;

            dataPoints.Add(new EvolutionDataPointDTO
            {
                ScanId = scan.Id,
                ScanDate = scan.UploadDate,
                CsfVolume = ar.CsfVolume,
                GmVolume = ar.GmVolume,
                WmVolume = ar.WmVolume,
                TotalBrainVolume = ar.CsfVolume + ar.GmVolume + ar.WmVolume,
                AsymmetryIndex = ar.AsymmetryIndex,
                EpilepsyRiskScore = ar.EpilepsyRiskScore,
                TumorDetected = ar.TumorDetected,
                TumorVolume = ar.TumorVolume,
                TumorSurfaceArea = ar.TumorSurfaceArea,
                CortexThicknessAvg = ar.CortexThicknessAvg,
                WmDensityScore = ar.WmDensityScore
            });
        }

        // Calculate evolution summary
        var summary = new EvolutionSummaryDTO { TotalScans = dataPoints.Count };
        if (dataPoints.Count >= 2)
        {
            var first = dataPoints.First();
            var last = dataPoints.Last();
            var months = (last.ScanDate - first.ScanDate).TotalDays / 30.0;
            summary.MonthsSpan = Math.Round(months, 1);

            summary.BrainVolumeDelta = Math.Round(last.TotalBrainVolume - first.TotalBrainVolume, 3);
            summary.GmVolumeDelta = Math.Round(last.GmVolume - first.GmVolume, 3);
            summary.WmVolumeDelta = Math.Round(last.WmVolume - first.WmVolume, 3);
            summary.CsfVolumeDelta = Math.Round(last.CsfVolume - first.CsfVolume, 3);
            summary.TumorVolumeDelta = Math.Round(last.TumorVolume - first.TumorVolume, 3);
            summary.CortexThicknessDelta = Math.Round(last.CortexThicknessAvg - first.CortexThicknessAvg, 3);
            summary.WmDensityDelta = Math.Round(last.WmDensityScore - first.WmDensityScore, 1);

            summary.BrainVolumeChangeRate = months > 0
                ? Math.Round(summary.BrainVolumeDelta / months, 4)
                : 0;

            // Degradation classification based on brain volume loss rate
            var monthlyLoss = Math.Abs(summary.BrainVolumeChangeRate);
            summary.DegradationLevel = monthlyLoss switch
            {
                > 0.05 => "Severe",
                > 0.02 => "Moderate",
                > 0.005 => "Mild",
                _ => "Stable"
            };
        }

        // LLM interpretation of evolution
        string? llmInterpretation = null;
        if (dataPoints.Count >= 2)
        {
            try
            {
                llmInterpretation = await GenerateEvolutionInterpretation(
                    $"{patient.FirstName} {patient.LastName}",
                    CalculateAge(patient.DateOfBirth),
                    dataPoints, summary);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate LLM evolution interpretation");
            }
        }

        return new PatientEvolutionDTO
        {
            PatientId = patientId,
            PatientName = $"{patient.FirstName} {patient.LastName}",
            DataPoints = dataPoints,
            Summary = summary,
            LlmInterpretation = llmInterpretation
        };
    }

    private async Task<string> GenerateEvolutionInterpretation(
        string patientName, int age,
        List<EvolutionDataPointDTO> dataPoints, EvolutionSummaryDTO summary)
    {
        var dataPointsSummary = string.Join("\n", dataPoints.Select((dp, i) =>
            $"Scan {i + 1} ({dp.ScanDate:yyyy-MM-dd}): Brain={dp.TotalBrainVolume:F2}cm³, " +
            $"GM={dp.GmVolume:F2}, WM={dp.WmVolume:F2}, CSF={dp.CsfVolume:F2}, " +
            $"Asymmetry={dp.AsymmetryIndex:F2}%, Risk={dp.EpilepsyRiskScore:F1}/100, " +
            $"Tumor={dp.TumorVolume:F3}cm³, Cortex={dp.CortexThicknessAvg:F2}, WM-Density={dp.WmDensityScore:F1}"));

        var prompt = $@"Patient: {patientName}, Age: {age}
Longitudinal MRI Analysis ({summary.TotalScans} scans over {summary.MonthsSpan:F1} months)

=== SCAN DATA ===
{dataPointsSummary}

=== EVOLUTION SUMMARY ===
Brain Volume Change: {summary.BrainVolumeDelta:+0.000;-0.000} cm³ ({summary.BrainVolumeChangeRate:+0.0000;-0.0000} cm³/month)
GM Change: {summary.GmVolumeDelta:+0.000;-0.000} cm³
WM Change: {summary.WmVolumeDelta:+0.000;-0.000} cm³
CSF Change: {summary.CsfVolumeDelta:+0.000;-0.000} cm³
Tumor Volume Change: {summary.TumorVolumeDelta:+0.000;-0.000} cm³
Cortex Thickness Change: {summary.CortexThicknessDelta:+0.000;-0.000}
WM Density Change: {summary.WmDensityDelta:+0.0;-0.0}
Degradation Level: {summary.DegradationLevel}

Please generate a longitudinal clinical interpretation with:
1. VOLUME EVOLUTION TREND — Is the brain shrinking? At what rate?
2. TISSUE COMPOSITION CHANGES — GM/WM/CSF balance over time
3. TUMOR PROGRESSION — Any growth or new detection?
4. CORTICAL ASSESSMENT — Cortex thickness trend
5. WHITE MATTER INTEGRITY — Density changes
6. CLINICAL SIGNIFICANCE — What does this evolution pattern mean for epilepsy?
7. RECOMMENDATION — Suggested follow-up interval and actions";

        var patientContext = new PatientContextDTO
        {
            PatientName = patientName,
            Age = age,
            ScanDate = dataPoints.Last().ScanDate
        };

        // Reuse OpenAI service with evolution-specific prompt
        var systemPrompt = @"You are a neuroradiology specialist analyzing longitudinal brain MRI data.
Focus on temporal trends: volume changes, degradation speed, tumor growth, and cortical thinning.
Provide actionable clinical insights for epilepsy monitoring in pediatric patients.";

        // Call OpenAI directly for evolution
        return await _openAiReportService.GenerateEvolutionReportAsync(prompt, systemPrompt);
    }
}
