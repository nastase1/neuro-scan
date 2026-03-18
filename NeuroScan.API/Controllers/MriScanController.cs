using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroScan.Application.IServices;
namespace NeuroScan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MriScanController : ControllerBase
{
    private readonly IMriScanService _mriScanService;
    private readonly ILogger<MriScanController> _logger;

    public MriScanController(IMriScanService mriScanService, ILogger<MriScanController> logger)
    {
        _mriScanService = mriScanService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a new MRI scan for a patient (Doctor only)
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Roles = "Doctor")]
    [RequestSizeLimit(500 * 1024 * 1024)] // 500MB limit
    public async Task<ActionResult<MriScanResponseDTO>> UploadScan([FromForm] MriScanUploadDTO uploadDto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (uploadDto.File == null || uploadDto.File.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        if (!uploadDto.File.FileName.EndsWith(".nii", StringComparison.OrdinalIgnoreCase) &&
            !uploadDto.File.FileName.EndsWith(".nii.gz", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only .nii or .nii.gz files are allowed" });
        }

        try
        {
            var result = await _mriScanService.UploadAndProcessScanAsync(uploadDto, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading MRI scan");
            return StatusCode(500, new { error = $"Upload failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Upload a new MRI scan for yourself (Standard User only)
    /// </summary>
    [HttpPost("upload-self")]
    [Authorize(Roles = "StandardUser")]
    [RequestSizeLimit(500 * 1024 * 1024)] // 500MB limit
    public async Task<ActionResult<MriScanResponseDTO>> UploadSelfScan([FromForm] IFormFile file, [FromForm] string? notes = null)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        if (!file.FileName.EndsWith(".nii", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".nii.gz", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only .nii or .nii.gz files are allowed" });
        }

        try
        {
            var result = await _mriScanService.UploadSelfScanAsync(file, notes, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading MRI scan");
            return StatusCode(500, new { error = $"Upload failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get all scans for a specific patient (Doctor or linked Standard User)
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [Authorize(Roles = "Doctor,StandardUser")]
    public async Task<ActionResult<IEnumerable<MriScanDetailDTO>>> GetPatientScans(Guid patientId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        try
        {
            var scans = await _mriScanService.GetScansByPatientIdAsync(patientId, userId, userRole == "Doctor");
            return Ok(scans);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient scans");
            return StatusCode(500, new { error = "Failed to retrieve scans" });
        }
    }

    /// <summary>
    /// Get scan details with analysis results (Doctors can view any scan)
    /// </summary>
    [HttpGet("{scanId}")]
    public async Task<ActionResult<MriScanDetailDTO>> GetScanDetails(Guid scanId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        // Doctors can access any scan, standard users only their own patients' scans
        var result = await _mriScanService.GetScanDetailsAsync(scanId, userId, userRole == "Doctor");

        if (result == null)
        {
            return NotFound(new { error = "Scan not found or access denied" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Submit corrected segmentation mask (Doctor only)
    /// </summary>
    [HttpPost("{scanId}/correct-mask")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult> SubmitCorrectedMask(Guid scanId, [FromForm] IFormFile correctedMask)
    {
        var doctorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (correctedMask == null || correctedMask.Length == 0)
        {
            return BadRequest(new { error = "No mask file uploaded" });
        }

        try
        {
            await _mriScanService.SubmitCorrectedMaskAsync(scanId, correctedMask, doctorId);
            return Ok(new { message = "Corrected mask submitted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting corrected mask");
            return StatusCode(500, new { error = $"Submission failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get all scans belonging to the current user (Standard User)
    /// </summary>
    [HttpGet("my-scans")]
    public async Task<ActionResult<IEnumerable<MriScanDetailDTO>>> GetMyScans()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var scans = await _mriScanService.GetMyScansAsync(userId);
        return Ok(scans);
    }

    /// <summary>
    /// Get scans pending doctor review
    /// </summary>
    [HttpGet("pending-review")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<IEnumerable<MriScanSummaryDTO>>> GetPendingReviewScans()
    {
        var scans = await _mriScanService.GetPendingReviewScansAsync();
        return Ok(scans);
    }

    /// <summary>
    /// Get a single colored segmentation slice by index
    /// </summary>
    [HttpGet("{scanId}/segmentation-image/{sliceIndex:int}")]
    public async Task<ActionResult> GetSegmentationSlice(Guid scanId, int sliceIndex)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        var scan = await _mriScanService.GetScanDetailsAsync(scanId, userId, userRole == "Doctor");
        if (scan == null) return NotFound(new { error = "Scan not found or access denied" });

        var sliceDir = scan.AnalysisResult?.SegmentationImagePath;
        var sliceCount = scan.AnalysisResult?.SegmentationSliceCount ?? 0;

        if (string.IsNullOrEmpty(sliceDir) || sliceCount == 0)
            return NotFound(new { error = "Segmentation slices not available" });

        if (sliceIndex < 0 || sliceIndex >= sliceCount)
            return BadRequest(new { error = $"Slice index must be between 0 and {sliceCount - 1}" });

        var slicePath = Path.Combine(sliceDir, $"{sliceIndex}.png");
        if (!System.IO.File.Exists(slicePath))
            return NotFound(new { error = "Slice file not found" });

        var bytes = await System.IO.File.ReadAllBytesAsync(slicePath);
        return File(bytes, "image/png");
    }

    /// <summary>
    /// Get count of raw grayscale MRI slices (triggers generation on first call). Doctor only.
    /// </summary>
    [HttpGet("{scanId}/raw-slice/count")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<int>> GetRawSliceCount(Guid scanId)
    {
        var doctorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var count = await _mriScanService.GetRawSliceCountAsync(scanId, doctorId);
        return Ok(new { count });
    }

    /// <summary>
    /// Get a single raw grayscale MRI slice. Doctor only.
    /// </summary>
    [HttpGet("{scanId}/raw-slice/{sliceIndex:int}")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult> GetRawSlice(Guid scanId, int sliceIndex)
    {
        var doctorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var bytes = await _mriScanService.GetRawSliceAsync(scanId, sliceIndex, doctorId);
        if (bytes == null) return NotFound(new { error = "Slice not available" });
        return File(bytes, "image/png");
    }

    /// <summary>
    /// Submit doctor review: approve/reject AI result with optional notes. Doctor only.
    /// </summary>
    [HttpPost("{scanId}/review")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult> SubmitReview(Guid scanId, [FromBody] DoctorReviewDTO dto)
    {
        var doctorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            await _mriScanService.SubmitReviewAsync(scanId, doctorId, dto.Approved, dto.Notes ?? string.Empty);
            return Ok(new { message = "Review submitted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting review");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Save a doctor-painted corrected segmentation slice. Doctor only.
    /// </summary>
    [HttpPost("{scanId}/corrected-slice/{sliceIndex:int}")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult> SaveCorrectedSlice(Guid scanId, int sliceIndex, [FromBody] CorrectedSliceDTO dto)
    {
        var doctorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            await _mriScanService.SaveCorrectedSliceAsync(scanId, sliceIndex, dto.Base64Png, doctorId);
            return Ok(new { message = "Corrected slice saved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving corrected slice");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get a doctor-corrected slice image. Doctor only.
    /// </summary>
    [HttpGet("{scanId}/corrected-slice/{sliceIndex:int}")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult> GetCorrectedSlice(Guid scanId, int sliceIndex)
    {
        var doctorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var bytes = await _mriScanService.GetCorrectedSliceAsync(scanId, sliceIndex, doctorId);
        if (bytes == null) return NoContent();
        return File(bytes, "image/png");
    }
}

public class DoctorReviewDTO
{
    public bool Approved { get; set; }
    public string? Notes { get; set; }
}

public class CorrectedSliceDTO
{
    public string Base64Png { get; set; } = string.Empty;
}
