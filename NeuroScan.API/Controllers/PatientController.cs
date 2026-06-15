using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroScan.Application.IServices;
using NeuroScan.Application.DTOs;

namespace NeuroScan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly ILogger<PatientController> _logger;

    public PatientController(IPatientService patientService, ILogger<PatientController> logger)
    {
        _patientService = patientService;
        _logger = logger;
    }

    /// <summary>
    /// Get all patients created by the current doctor (Doctor-only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<IEnumerable<PatientDTO>>> GetAllPatients()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var patients = await _patientService.GetAllByUserAsync(userId);
        return Ok(patients);
    }

    /// <summary>
    /// Get the current user's patient record (for standard users)
    /// </summary>
    [HttpGet("my-patient")]
    [Authorize(Roles = "StandardUser")]
    public async Task<ActionResult<PatientDTO>> GetMyPatient()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var patient = await _patientService.GetMyPatientAsync(userId);

        if (patient == null)
        {
            return NotFound(new { error = "Patient record not found" });
        }

        return Ok(patient);
    }

    /// <summary>
    /// Get a specific patient by ID (Doctor-only, only their own patients)
    /// </summary>
    [HttpGet("{patientId}")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<PatientDTO>> GetPatient(Guid patientId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var patient = await _patientService.GetByIdAsync(patientId, userId);

        if (patient == null)
        {
            return NotFound(new { error = "Patient not found or access denied" });
        }

        return Ok(patient);
    }

    /// <summary>
    /// Create a new patient (Doctor-only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<PatientDTO>> CreatePatient([FromBody] CreatePatientDTO dto)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var patient = await _patientService.CreatePatientAsync(dto, userId);
            return CreatedAtAction(nameof(GetPatient), new { patientId = patient.Id }, patient);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating patient");
            return StatusCode(500, new { error = "Failed to create patient" });
        }
    }

    /// <summary>
    /// Update an existing patient (Doctor-only, only their own patients)
    /// </summary>
    [HttpPut("{patientId}")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<PatientDTO>> UpdatePatient(Guid patientId, [FromBody] UpdatePatientDTO dto)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var patient = await _patientService.UpdatePatientAsync(patientId, dto, userId);

            if (patient == null)
            {
                return NotFound(new { error = "Patient not found or access denied" });
            }

            return Ok(patient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating patient");
            return StatusCode(500, new { error = "Failed to update patient" });
        }
    }
}
