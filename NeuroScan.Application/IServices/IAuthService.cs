using NeuroScan.Domain.Entities;

namespace NeuroScan.Application.IServices;

public interface IAuthService
{
    Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO request);
    Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request);
    Task<User?> GetCurrentUserAsync(Guid userId);
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
}
