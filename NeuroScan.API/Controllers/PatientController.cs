using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroScan.Application.IServices;

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
    /// Get all patients created by the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientDTO>>> GetAllPatients()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var patients = await _patientService.GetAllByUserAsync(userId);
        return Ok(patients);
    }

    /// <summary>
    /// Get a specific patient by ID
    /// </summary>
    [HttpGet("{patientId}")]
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
    /// Create a new patient
    /// </summary>
    [HttpPost]
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
    /// Update an existing patient
    /// </summary>
    [HttpPut("{patientId}")]
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
