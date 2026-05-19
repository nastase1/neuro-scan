using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeuroScan.Application.Constants;
using NeuroScan.Application.Helpers;
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
    private readonly IScanProcessingService _scanProcessingService;
    private readonly IMriImageService _mriImageService;
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
        IScanProcessingService scanProcessingService,
        IMriImageService mriImageService,
        ILogger<MriScanService> logger,
        IConfiguration configuration)
    {
        _mriScanRepository = mriScanRepository;
        _patientRepository = patientRepository;
        _userRepository = userRepository;
        _analysisResultRepository = analysisResultRepository;
        _aiAnalysisService = aiAnalysisService;
        _openAiReportService = openAiReportService;
        _scanProcessingService = scanProcessingService;
        _mriImageService = mriImageService;
        _logger = logger;
        _uploadPath = configuration["Storage:UploadPath"] ?? "uploads/scans";
        _trainingDataPath = configuration["Storage:TrainingDataPath"] ?? "uploads/training-data";

        Directory.CreateDirectory(_uploadPath);
        Directory.CreateDirectory(_trainingDataPath);
    }

    public async Task<MriScanResponseDTO> UploadAndProcessScanAsync(MriScanUploadDTO uploadDto, Guid userId)
    {
        var patient = await _patientRepository.GetByIdAsync(uploadDto.PatientId);
        if (patient == null || patient.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Patient not found or access denied");

        var fileName = $"{Guid.NewGuid()}_{uploadDto.File.FileName}";
        var filePath = Path.Combine(_uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await uploadDto.File.CopyToAsync(stream);

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
        _scanProcessingService.StartProcessingScan(mriScan.Id);

        return new MriScanResponseDTO
        {
            ScanId = mriScan.Id,
            Message = "Scan uploaded successfully. Processing started.",
            Status = ScanStatus.Processing
        };
    }

    public async Task<MriScanResponseDTO> UploadSelfScanAsync(IFormFile file, string? notes, Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new UnauthorizedAccessException("User not found");

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
                MedicalRecordNumber = $"{ScanConstants.MrnSelfPrefix}{userId.ToString()[..8].ToUpper()}",
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _patientRepository.AddAsync(patient);
        }

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(_uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

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
        _scanProcessingService.StartProcessingScan(mriScan.Id);

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
            throw new UnauthorizedAccessException("Patient not found or access denied");

        var t1Path = await SaveFileAsync(uploadDto.T1File);
        var t1cePath = await SaveFileAsync(uploadDto.T1ceFile);
        var t2Path = await SaveFileAsync(uploadDto.T2File);
        var flairPath = await SaveFileAsync(uploadDto.FlairFile);

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
        _scanProcessingService.StartProcessingTumorScan(mriScan.Id);

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
            throw new UnauthorizedAccessException("User not found");

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
                MedicalRecordNumber = $"{ScanConstants.MrnSelfPrefix}{userId.ToString()[..8].ToUpper()}",
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _patientRepository.AddAsync(patient);
        }

        var t1Path = await SaveFileAsync(t1);
        var t1cePath = await SaveFileAsync(t1ce);
        var t2Path = await SaveFileAsync(t2);
        var flairPath = await SaveFileAsync(flair);

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
        _scanProcessingService.StartProcessingTumorScan(mriScan.Id);

        return new MriScanResponseDTO
        {
            ScanId = mriScan.Id,
            Message = "Tumor scan uploaded successfully. Processing started.",
            Status = ScanStatus.Processing
        };
    }

    public async Task<MriScanDetailDTO?> GetScanDetailsAsync(Guid scanId, Guid userId, bool isDoctor = false)
    {
        var scan = await _mriScanRepository.GetByIdAsync(scanId);
        if (scan == null) return null;

        var patient = await _patientRepository.GetByIdAsync(scan.PatientId);
        if (patient == null) return null;

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
        if (analysisResult.SegmentationSliceCount >= ScanConstants.FullSegmentationSliceCount &&
            !string.IsNullOrWhiteSpace(analysisResult.SegmentationImagePath) &&
            Directory.Exists(analysisResult.SegmentationImagePath))
        {
            return;
        }

        try
        {
            var aiResult = await _aiAnalysisService.AnalyzeMriScanAsync(scan.StoredFilePath);
            if (aiResult.SegmentationSlices.Count == 0) return;

            var (segDir, segCount) = await _mriImageService.SaveSegmentationSlicesAsync(scan.Id, aiResult.SegmentationSlices);
            analysisResult.SegmentationImagePath = segDir;
            analysisResult.SegmentationSliceCount = segCount;

            if (aiResult.TumorOverlaySlices.Count > 0)
            {
                var (overlayDir, overlayCount) = await _mriImageService.SaveTumorOverlaySlicesAsync(scan.Id, aiResult.TumorOverlaySlices);
                analysisResult.TumorOverlayImagePath = overlayDir;
                analysisResult.TumorOverlaySliceCount = overlayCount;
            }

            analysisResult.UpdatedAt = DateTime.UtcNow;
            await _analysisResultRepository.UpdateAsync(analysisResult);

            _logger.LogInformation("Regenerated aligned segmentation slices for legacy scan {ScanId}", scan.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to regenerate segmentation slices for scan {ScanId}", scan.Id);
        }
    }

    public async Task<IEnumerable<MriScanDetailDTO>> GetScansByPatientIdAsync(Guid patientId, Guid requesterId, bool isDoctor = false)
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

        return scanDetails.OrderByDescending(s => s.UploadDate);
    }

    public async Task SubmitCorrectedMaskAsync(Guid scanId, IFormFile correctedMask, Guid doctorId)
    {
        var scan = await _mriScanRepository.GetByIdAsync(scanId);
        if (scan == null) throw new ArgumentException("Scan not found");

        var fileName = $"corrected_{scanId}_{correctedMask.FileName}";
        var filePath = Path.Combine(_trainingDataPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await correctedMask.CopyToAsync(stream);

        scan.CorrectedMaskPath = filePath;
        scan.ReviewedByDoctorId = doctorId;
        scan.ReviewedAt = DateTime.UtcNow;
        scan.Status = ScanStatus.ReviewedByDoctor;
        scan.UpdatedAt = DateTime.UtcNow;

        await _mriScanRepository.UpdateAsync(scan);
        _logger.LogInformation("Doctor {DoctorId} submitted corrected mask for scan {ScanId}", doctorId, scanId);
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
        int? expectedArg = expectedSliceCount == 0 ? null : expectedSliceCount;

        if (await _mriImageService.HasRawSliceCacheAsync(scanId, expectedArg))
            return await _mriImageService.GetCachedRawSliceCountAsync(scanId);

        await _mriImageService.InvalidateRawSliceCacheAsync(scanId);

        if (!File.Exists(scan.StoredFilePath)) return 0;

        var slices = await _aiAnalysisService.GetRawSlicesAsync(scan.StoredFilePath);
        if (slices.Count == 0) return 0;

        return await _mriImageService.SaveRawSlicesAsync(scanId, slices);
    }

    public async Task<byte[]?> GetRawSliceAsync(Guid scanId, int sliceIndex, Guid doctorId)
    {
        if (!await _mriImageService.HasRawSliceCacheAsync(scanId, null))
        {
            var count = await GetRawSliceCountAsync(scanId, doctorId);
            if (count == 0) return null;
        }

        return await _mriImageService.GetRawSliceAsync(scanId, sliceIndex);
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

        _logger.LogInformation("Doctor {DoctorId} submitted review for scan {ScanId} — approved: {Approved}", doctorId, scanId, approved);
    }

    public async Task SaveCorrectedSliceAsync(Guid scanId, int sliceIndex, string base64Png, Guid doctorId)
    {
        var scan = await _mriScanRepository.GetByIdAsync(scanId);
        if (scan == null) throw new ArgumentException("Scan not found");

        await _mriImageService.SaveCorrectedSliceAsync(scanId, sliceIndex, base64Png);
        _logger.LogInformation("Doctor {DoctorId} saved corrected slice {SliceIndex} for scan {ScanId}", doctorId, sliceIndex, scanId);
    }

    public async Task<byte[]?> GetCorrectedSliceAsync(Guid scanId, int sliceIndex, Guid doctorId)
    {
        return await _mriImageService.GetCorrectedSliceAsync(scanId, sliceIndex);
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

            var monthlyLoss = Math.Abs(summary.BrainVolumeChangeRate);
            summary.DegradationLevel = monthlyLoss switch
            {
                > 0.05 => "Severe",
                > 0.02 => "Moderate",
                > 0.005 => "Mild",
                _ => "Stable"
            };
        }

        string? llmInterpretation = null;
        if (dataPoints.Count >= 2)
        {
            try
            {
                llmInterpretation = await GenerateEvolutionInterpretation(
                    $"{patient.FirstName} {patient.LastName}",
                    DateHelper.CalculateAge(patient.DateOfBirth),
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

        var systemPrompt = @"You are a neuroradiology specialist analyzing longitudinal brain MRI data.
Focus on temporal trends: volume changes, degradation speed, tumor growth, and cortical thinning.
Provide actionable clinical insights for epilepsy monitoring in pediatric patients.";

        return await _openAiReportService.GenerateEvolutionReportAsync(prompt, systemPrompt);
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

    private async Task<string> SaveFileAsync(IFormFile file)
    {
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(_uploadPath, fileName);
        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);
        return filePath;
    }
}
