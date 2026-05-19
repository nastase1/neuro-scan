using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeuroScan.Application.Helpers;
using NeuroScan.Application.IServices;
using NeuroScan.Domain.Entities;
using NeuroScan.Domain.IRepositories;

namespace NeuroScan.Application.Services;

public class ScanProcessingService : IScanProcessingService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ScanProcessingService> _logger;

    public ScanProcessingService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ScanProcessingService> logger,
        IConfiguration configuration)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public void StartProcessingScan(Guid scanId)
    {
        _ = Task.Run(async () => await ProcessScanAsync(scanId));
    }

    public void StartProcessingTumorScan(Guid scanId)
    {
        _ = Task.Run(async () => await ProcessTumorScanAsync(scanId));
    }

    private async Task ProcessScanAsync(Guid scanId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var mriScanRepository = scope.ServiceProvider.GetRequiredService<IMriScanRepository>();
        var patientRepository = scope.ServiceProvider.GetRequiredService<IPatientRepository>();
        var analysisResultRepository = scope.ServiceProvider.GetRequiredService<IAnalysisResultRepository>();
        var aiAnalysisService = scope.ServiceProvider.GetRequiredService<IAiAnalysisService>();
        var openAiReportService = scope.ServiceProvider.GetRequiredService<IOpenAiReportService>();
        var mriImageService = scope.ServiceProvider.GetRequiredService<IMriImageService>();

        try
        {
            var mriScan = await mriScanRepository.GetByIdAsync(scanId);
            if (mriScan == null)
            {
                _logger.LogError("Scan {ScanId} not found", scanId);
                return;
            }

            mriScan.Status = ScanStatus.Processing;
            await mriScanRepository.UpdateAsync(mriScan);

            _logger.LogInformation("Calling AI service for scan {ScanId}", mriScan.Id);
            var aiResult = await aiAnalysisService.AnalyzeMriScanAsync(mriScan.StoredFilePath);

            var accurateRiskLevel = GetEpilepsyRiskLevel(aiResult.Epilepsy.RiskScore);
            aiResult.Epilepsy.RiskLevel = accurateRiskLevel;

            var patient = await patientRepository.GetByIdAsync(mriScan.PatientId);
            if (patient == null)
            {
                _logger.LogError("Patient {PatientId} not found for scan {ScanId}", mriScan.PatientId, scanId);
                mriScan.Status = ScanStatus.Failed;
                await mriScanRepository.UpdateAsync(mriScan);
                return;
            }

            var patientContext = new PatientContextDTO
            {
                PatientName = $"{patient.FirstName} {patient.LastName}",
                Age = DateHelper.CalculateAge(patient.DateOfBirth),
                ScanDate = mriScan.UploadDate
            };

            _logger.LogInformation("Generating medical report for scan {ScanId}", mriScan.Id);
            string medicalReport;
            try
            {
                medicalReport = await openAiReportService.GenerateMedicalReportAsync(aiResult, patientContext);
            }
            catch (Exception reportEx)
            {
                _logger.LogWarning(reportEx, "Failed to generate OpenAI report for scan {ScanId}. Using fallback.", mriScan.Id);
                medicalReport = GenerateFallbackReport(aiResult, patientContext);
            }

            string? segImageDir = null;
            int segSliceCount = 0;
            if (aiResult.SegmentationSlices.Count > 0)
            {
                try
                {
                    (segImageDir, segSliceCount) = await mriImageService.SaveSegmentationSlicesAsync(mriScan.Id, aiResult.SegmentationSlices);
                }
                catch (Exception imgEx)
                {
                    _logger.LogWarning(imgEx, "Failed to save segmentation slices for scan {ScanId}", mriScan.Id);
                }
            }

            string? tumorOverlayDir = null;
            int tumorOverlayCount = 0;
            if (aiResult.TumorOverlaySlices.Count > 0)
            {
                try
                {
                    (tumorOverlayDir, tumorOverlayCount) = await mriImageService.SaveTumorOverlaySlicesAsync(mriScan.Id, aiResult.TumorOverlaySlices);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save tumor overlay slices for scan {ScanId}", mriScan.Id);
                }
            }

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

            _logger.LogInformation("Scan {ScanId} processed successfully — Epilepsy risk: {RiskLevel}", mriScan.Id, accurateRiskLevel);

            await TrySendScanResultsEmailAsync(scope, patient, mriScan, medicalReport, aiResult, accurateRiskLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scan {ScanId}", scanId);
            await TryUpdateScanStatusAsync(mriScanRepository, scanId, ScanStatus.Failed);
        }
    }

    private async Task ProcessTumorScanAsync(Guid scanId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var mriScanRepository = scope.ServiceProvider.GetRequiredService<IMriScanRepository>();
        var patientRepository = scope.ServiceProvider.GetRequiredService<IPatientRepository>();
        var analysisResultRepository = scope.ServiceProvider.GetRequiredService<IAnalysisResultRepository>();
        var aiAnalysisService = scope.ServiceProvider.GetRequiredService<IAiAnalysisService>();
        var openAiReportService = scope.ServiceProvider.GetRequiredService<IOpenAiReportService>();
        var mriImageService = scope.ServiceProvider.GetRequiredService<IMriImageService>();

        try
        {
            var mriScan = await mriScanRepository.GetByIdAsync(scanId);
            if (mriScan == null)
            {
                _logger.LogError("Tumor scan {ScanId} not found", scanId);
                return;
            }

            mriScan.Status = ScanStatus.Processing;
            await mriScanRepository.UpdateAsync(mriScan);

            _logger.LogInformation("Calling AI tumor service for scan {ScanId}", mriScan.Id);
            var aiResult = await aiAnalysisService.AnalyzeTumorScanAsync(
                mriScan.StoredFilePath,
                mriScan.StoredFilePathT1ce!,
                mriScan.StoredFilePathT2!,
                mriScan.StoredFilePathFlair!);

            var accurateRiskLevel = GetEpilepsyRiskLevel(aiResult.Epilepsy.RiskScore);
            aiResult.Epilepsy.RiskLevel = accurateRiskLevel;

            var patient = await patientRepository.GetByIdAsync(mriScan.PatientId);
            if (patient == null)
            {
                _logger.LogError("Patient {PatientId} not found for tumor scan {ScanId}", mriScan.PatientId, scanId);
                mriScan.Status = ScanStatus.Failed;
                await mriScanRepository.UpdateAsync(mriScan);
                return;
            }

            var patientContext = new PatientContextDTO
            {
                PatientName = $"{patient.FirstName} {patient.LastName}",
                Age = DateHelper.CalculateAge(patient.DateOfBirth),
                ScanDate = mriScan.UploadDate
            };

            _logger.LogInformation("Generating medical report for tumor scan {ScanId}", mriScan.Id);
            string medicalReport;
            try
            {
                medicalReport = await openAiReportService.GenerateMedicalReportAsync(aiResult, patientContext);
            }
            catch (Exception reportEx)
            {
                _logger.LogWarning(reportEx, "Failed to generate OpenAI report for tumor scan {ScanId}. Using fallback.", mriScan.Id);
                medicalReport = GenerateFallbackReport(aiResult, patientContext);
            }

            string? segImageDir = null;
            int segSliceCount = 0;
            if (aiResult.SegmentationSlices.Count > 0)
            {
                try
                {
                    (segImageDir, segSliceCount) = await mriImageService.SaveSegmentationSlicesAsync(mriScan.Id, aiResult.SegmentationSlices);
                }
                catch (Exception imgEx)
                {
                    _logger.LogWarning(imgEx, "Failed to save segmentation slices for tumor scan {ScanId}", mriScan.Id);
                }
            }

            string? tumorOverlayDir = null;
            int tumorOverlayCount = 0;
            if (aiResult.TumorOverlaySlices.Count > 0)
            {
                try
                {
                    (tumorOverlayDir, tumorOverlayCount) = await mriImageService.SaveTumorOverlaySlicesAsync(mriScan.Id, aiResult.TumorOverlaySlices);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save tumor overlay slices for tumor scan {ScanId}", mriScan.Id);
                }
            }

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

            _logger.LogInformation("Tumor scan {ScanId} processed successfully — Tumor detected: {TumorDetected}", mriScan.Id, aiResult.Tumor.TumorDetected);

            await TrySendScanResultsEmailAsync(scope, patient, mriScan, medicalReport, aiResult, accurateRiskLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing tumor scan {ScanId}", scanId);
            await TryUpdateScanStatusAsync(mriScanRepository, scanId, ScanStatus.Failed);
        }
    }

    private async Task TrySendScanResultsEmailAsync(
        IServiceScope scope, Patient patient, MriScan mriScan,
        string medicalReport, SegResNetAnalysisResponseDTO aiResult, string accurateRiskLevel)
    {
        if (string.IsNullOrWhiteSpace(patient.Email)) return;

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
            _logger.LogWarning(emailEx, "Failed to send scan results email to patient {PatientId}", patient.Id);
        }
    }

    private async Task TryUpdateScanStatusAsync(IMriScanRepository mriScanRepository, Guid scanId, ScanStatus status)
    {
        try
        {
            var mriScan = await mriScanRepository.GetByIdAsync(scanId);
            if (mriScan != null)
            {
                mriScan.Status = status;
                await mriScanRepository.UpdateAsync(mriScan);
            }
        }
        catch (Exception updateEx)
        {
            _logger.LogError(updateEx, "Failed to update scan status for {ScanId}", scanId);
        }
    }

    private static string GetEpilepsyRiskLevel(double riskScore) => riskScore switch
    {
        >= 70 => "High",
        >= 40 => "Moderate",
        _ => "Low"
    };

    private static string GenerateFallbackReport(SegResNetAnalysisResponseDTO analysisData, PatientContextDTO patientContext)
    {
        var riskFactorsText = string.Join("\n- ", analysisData.Epilepsy.Factors);

        return $@"=== NEUROSCAN AUTOMATED ANALYSIS REPORT ===

Patient: {patientContext.PatientName}
Age: {patientContext.Age}
Scan Date: {patientContext.ScanDate:yyyy-MM-dd}

=== VOLUMETRIC FINDINGS ===
CSF Volume:          {analysisData.Segresnet.CsfVolume:F2} cm³
Gray Matter Volume:  {analysisData.Segresnet.GmVolume:F2} cm³
White Matter Volume: {analysisData.Segresnet.WmVolume:F2} cm³
Brain Asymmetry Index: {analysisData.Segresnet.AsymmetryIndex:F4}%

=== EPILEPSY RISK ASSESSMENT ===
Risk Level: {analysisData.Epilepsy.RiskLevel}
Risk Score: {analysisData.Epilepsy.RiskScore:F1}/100

Factors Detected:
{riskFactorsText}

=== ADDITIONAL METRICS ===
Cortex Thickness: Avg {analysisData.CortexThickness.AvgThickness:F2}mm (Min: {analysisData.CortexThickness.MinThickness:F2}mm, Max: {analysisData.CortexThickness.MaxThickness:F2}mm)
White Matter Density Score: {analysisData.WhiteMatterDensity.DensityScore:F2}
Tumor Detected: {(analysisData.Tumor.TumorDetected ? "Yes" : "No")}

=== CLINICAL RECOMMENDATION ===
This automated analysis provides preliminary volumetric measurements and risk assessment based on structural MRI data.
Clinical correlation with patient history, neurological examination, and EEG findings is essential for accurate diagnosis.
Findings suggestive of epilepsy risk should prompt further neurological evaluation.

=== DISCLAIMER ===
This report is generated by an AI-assisted analysis system and should be reviewed by a qualified neuroradiologist or neurologist.
It is intended to support, not replace, clinical judgment and diagnostic expertise.

NOTE: Advanced report generation temporarily unavailable. This is an automated fallback report based on quantitative analysis.
";
    }
}
