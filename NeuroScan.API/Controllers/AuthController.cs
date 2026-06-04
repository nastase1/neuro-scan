using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroScan.Application.IServices;

namespace NeuroScan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDTO>> Register([FromBody] RegisterRequestDTO request)
    {
        var result = await _authService.RegisterAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Login with Google OAuth credential
    /// </summary>
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponseDTO>> GoogleLogin([FromBody] GoogleAuthRequestDTO request)
    {
        var result = await _authService.GoogleLoginAsync(request);

        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDTO>> Login([FromBody] LoginRequestDTO request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Send password reset code to email
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<ActionResult<GenericResponseDTO>> ForgotPassword([FromBody] ForgotPasswordRequestDTO request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Reset password using 6-digit code
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<ActionResult<GenericResponseDTO>> ResetPassword([FromBody] ResetPasswordRequestDTO request)
    {
        var result = await _authService.ResetPasswordAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get the authenticated doctor's invite code
    /// </summary>
    [HttpGet("my-invite-code")]
    [Authorize]
    public async Task<ActionResult<object>> GetMyInviteCode()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var roleStr = User.FindFirstValue(ClaimTypes.Role);
        if (roleStr != "Doctor")
            return Forbid();

        var code = await _authService.GetMyInviteCodeAsync(userId);
        if (code == null) return NotFound();

        return Ok(new { inviteCode = code });
    }

    /// <summary>
    /// Update the authenticated user's profile
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UpdateProfileResponseDTO>> UpdateProfile([FromBody] UpdateProfileRequestDTO request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _authService.UpdateProfileAsync(userId, request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
