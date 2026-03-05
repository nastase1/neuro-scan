using NeuroScan.Domain.Entities;

namespace NeuroScan.Application.IServices;

public interface IAuthService
{
    Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO request);
    Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request);
    Task<User?> GetCurrentUserAsync(Guid userId);
    Task<GenericResponseDTO> ForgotPasswordAsync(ForgotPasswordRequestDTO request);
    Task<GenericResponseDTO> ResetPasswordAsync(ResetPasswordRequestDTO request);
    Task<string?> GetMyInviteCodeAsync(Guid doctorId);
}

public class AuthResponseDTO
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Message { get; set; }
    public UserDTO? User { get; set; }
}

public class RegisterRequestDTO
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public UserRole Role { get; set; } = UserRole.StandardUser;
    public string? InviteCode { get; set; }
}

public class LoginRequestDTO
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class UserDTO
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public UserRole Role { get; set; }
    public string? InviteCode { get; set; }
    public Guid? AssignedDoctorId { get; set; }
    public string? AssignedDoctorName { get; set; }
}

public class ForgotPasswordRequestDTO
{
    public required string Email { get; set; }
}

public class ResetPasswordRequestDTO
{
    public required string Email { get; set; }
    public required string Code { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmPassword { get; set; }
}

public class GenericResponseDTO
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
