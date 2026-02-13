using NeuroScan.Application.IServices;
using NeuroScan.Domain.Entities;
using NeuroScan.Domain.IRepositories;
using BCrypt.Net;

namespace NeuroScan.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(IUserRepository userRepository, IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO request)
    {
        // Check if user exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new AuthResponseDTO
            {
                Success = false,
                Message = "User with this email already exists"
            };
        }

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);

        var token = _jwtTokenService.GenerateToken(user);

        return new AuthResponseDTO
        {
            Success = true,
            Token = token,
            Message = "Registration successful",
            User = MapToUserDTO(user)
        };
    }

    public async Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResponseDTO
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        var token = _jwtTokenService.GenerateToken(user);

        return new AuthResponseDTO
        {
            Success = true,
            Token = token,
            Message = "Login successful",
            User = MapToUserDTO(user)
        };
    }

    public async Task<User?> GetCurrentUserAsync(Guid userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }

    private static UserDTO MapToUserDTO(User user)
    {
        return new UserDTO
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role
        };
    }
}
