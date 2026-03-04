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
    /// Get all scans for a specific patient (Doctor only)
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<IEnumerable<MriScanDetailDTO>>> GetPatientScans(Guid patientId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var scans = await _mriScanService.GetScansByPatientIdAsync(patientId, userId);
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
    /// Get scans pending doctor review
    /// </summary>
    [HttpGet("pending-review")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<IEnumerable<MriScanSummaryDTO>>> GetPendingReviewScans()
    {
        var scans = await _mriScanService.GetPendingReviewScansAsync();
        return Ok(scans);
    }
}
