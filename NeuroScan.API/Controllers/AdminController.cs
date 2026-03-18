using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroScan.Application.IServices;

namespace NeuroScan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    // GET /api/admin/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _adminService.GetStatsAsync();
        return Ok(stats);
    }

    // GET /api/admin/users?search=&role=&page=1&pageSize=20
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] int? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _adminService.GetUsersAsync(search, role, page, pageSize);
        return Ok(result);
    }

    // GET /api/admin/users/{id}
    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _adminService.GetUserByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    // PUT /api/admin/users/{id}
    [HttpPut("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUpdateUserDTO dto)
    {
        var success = await _adminService.UpdateUserAsync(id, dto);
        if (!success) return NotFound();
        return NoContent();
    }

    // DELETE /api/admin/users/{id}
    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var success = await _adminService.DeleteUserAsync(id);
        if (!success) return BadRequest("Cannot delete this user.");
        return NoContent();
    }

    // POST /api/admin/users/{id}/reset-password
    [HttpPost("users/{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] AdminResetPasswordDTO dto)
    {
        var success = await _adminService.ResetUserPasswordAsync(id, dto.NewPassword);
        if (!success) return NotFound();
        return NoContent();
    }

    // GET /api/admin/doctors
    [HttpGet("doctors")]
    public async Task<IActionResult> GetDoctors()
    {
        var doctors = await _adminService.GetDoctorsAsync();
        return Ok(doctors);
    }

    // GET /api/admin/scans?search=&status=&page=1&pageSize=20
    [HttpGet("scans")]
    public async Task<IActionResult> GetScans(
        [FromQuery] string? search,
        [FromQuery] int? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _adminService.GetScansAsync(search, status, page, pageSize);
        return Ok(result);
    }

    // DELETE /api/admin/scans/{id}
    [HttpDelete("scans/{id:guid}")]
    public async Task<IActionResult> DeleteScan(Guid id)
    {
        var success = await _adminService.DeleteScanAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}
