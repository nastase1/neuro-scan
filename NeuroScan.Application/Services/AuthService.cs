using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeuroScan.Application.Constants;
using NeuroScan.Application.IServices;
using NeuroScan.Application.DTOs;
using NeuroScan.Domain.Entities;
using NeuroScan.Domain.IRepositories;
using BCrypt.Net;

namespace NeuroScan.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    private readonly string _googleClientId;

    public AuthService(
        IUserRepository userRepository,
        IPatientRepository patientRepository,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        ILogger<AuthService> logger,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _patientRepository = patientRepository;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _logger = logger;
        _googleClientId = configuration["Google:ClientId"] ?? string.Empty;
    }

    public async Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO request)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new AuthResponseDTO
            {
                Success = false,
                Message = "User with this email already exists"
            };
        }

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

        if (request.Role == UserRole.Doctor)
        {
            user.InviteCode = GenerateInviteCode();
        }

        Guid? assignedDoctorId = null;
        if (request.Role == UserRole.StandardUser && !string.IsNullOrWhiteSpace(request.InviteCode))
        {
            var doctor = await _userRepository.GetByInviteCodeAsync(request.InviteCode.Trim().ToUpper());
            if (doctor == null)
            {
                return new AuthResponseDTO { Success = false, Message = "Invalid invite code. Please check with your doctor." };
            }
            user.AssignedDoctorId = doctor.Id;
            assignedDoctorId = doctor.Id;
        }

        await _userRepository.AddAsync(user);

        if (assignedDoctorId.HasValue)
        {
            var mrn = ScanConstants.MrnUserPrefix + user.Id.ToString("N")[..8].ToUpper();
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = DateTime.MinValue,
                MedicalRecordNumber = mrn,
                UserId = user.Id,
                CreatedByUserId = assignedDoctorId.Value,
                CreatedAt = DateTime.UtcNow
            };
            await _patientRepository.AddAsync(patient);
        }

        try
        {
            await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send welcome email to {Email}", user.Email);
        }

        var token = _jwtTokenService.GenerateToken(user);
        var dto = MapToUserDTO(user);
        if (user.AssignedDoctorId.HasValue)
        {
            var doctor = await _userRepository.GetByIdAsync(user.AssignedDoctorId.Value);
            if (doctor != null) dto.AssignedDoctorName = $"{doctor.FirstName} {doctor.LastName}";
        }

        return new AuthResponseDTO
        {
            Success = true,
            Token = token,
            Message = "Registration successful",
            User = dto
        };
    }

    public async Task<AuthResponseDTO> GoogleLoginAsync(GoogleAuthRequestDTO request)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_googleClientId]
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, settings);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid Google token");
            return new AuthResponseDTO { Success = false, Message = "Invalid Google token." };
        }

        var email = payload.Email;
        var googleId = payload.Subject;

        var user = await _userRepository.GetByEmailAsync(email);

        if (user != null)
        {
            // Link GoogleId daca nu era deja setat
            if (user.GoogleId != googleId)
            {
                user.GoogleId = googleId;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
            }
        }
        else
        {
            // Cont nou — StandardUser fara parola
            var nameParts = (payload.Name ?? email).Split(' ', 2);
            user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = nameParts[0],
                LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
                Email = email,
                GoogleId = googleId,
                Role = UserRole.StandardUser,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.AddAsync(user);

            try { await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to send welcome email to {Email}", user.Email); }
        }

        var token = _jwtTokenService.GenerateToken(user);
        var dto = MapToUserDTO(user);
        if (user.AssignedDoctorId.HasValue)
        {
            var doctor = await _userRepository.GetByIdAsync(user.AssignedDoctorId.Value);
            if (doctor != null) dto.AssignedDoctorName = $"{doctor.FirstName} {doctor.LastName}";
        }

        return new AuthResponseDTO { Success = true, Token = token, Message = "Login successful", User = dto };
    }

    public async Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || user.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResponseDTO
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        var token = _jwtTokenService.GenerateToken(user);
        var dto = MapToUserDTO(user);
        if (user.AssignedDoctorId.HasValue)
        {
            var doctor = await _userRepository.GetByIdAsync(user.AssignedDoctorId.Value);
            if (doctor != null) dto.AssignedDoctorName = $"{doctor.FirstName} {doctor.LastName}";
        }

        return new AuthResponseDTO
        {
            Success = true,
            Token = token,
            Message = "Login successful",
            User = dto
        };
    }

    public async Task<User?> GetCurrentUserAsync(Guid userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }

    public async Task<string?> GetMyInviteCodeAsync(Guid doctorId)
    {
        var doctor = await _userRepository.GetByIdAsync(doctorId);
        if (doctor == null || doctor.Role != UserRole.Doctor) return null;

        if (string.IsNullOrEmpty(doctor.InviteCode))
        {
            doctor.InviteCode = GenerateInviteCode();
            await _userRepository.UpdateAsync(doctor);
        }

        return doctor.InviteCode;
    }

    public async Task<UpdateProfileResponseDTO> UpdateProfileAsync(Guid userId, UpdateProfileRequestDTO request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return new UpdateProfileResponseDTO { Success = false, Message = "User not found" };

        if (!string.IsNullOrWhiteSpace(request.FirstName))
            user.FirstName = request.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(request.LastName))
            user.LastName = request.LastName.Trim();

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email.Trim() != user.Email)
        {
            var existing = await _userRepository.GetByEmailAsync(request.Email.Trim());
            if (existing != null)
                return new UpdateProfileResponseDTO { Success = false, Message = "Email already in use" };
            user.Email = request.Email.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return new UpdateProfileResponseDTO { Success = false, Message = "Current password is required to set a new password" };
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return new UpdateProfileResponseDTO { Success = false, Message = "Current password is incorrect" };
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        string? doctorName = null;
        if (user.AssignedDoctorId.HasValue)
        {
            var doctor = await _userRepository.GetByIdAsync(user.AssignedDoctorId.Value);
            doctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : null;
        }

        return new UpdateProfileResponseDTO
        {
            Success = true,
            Message = "Profile updated successfully",
            User = new UserDTO
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role,
                InviteCode = user.InviteCode,
                AssignedDoctorId = user.AssignedDoctorId,
                AssignedDoctorName = doctorName
            }
        };
    }

    public async Task<GenericResponseDTO> ForgotPasswordAsync(ForgotPasswordRequestDTO request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
        {
            return new GenericResponseDTO { Success = true, Message = "If this email is registered, a reset code has been sent." };
        }

        var code = new Random().Next(100000, 999999).ToString();
        user.PasswordResetCode = code;
        user.PasswordResetCodeExpiry = DateTime.UtcNow.AddMinutes(15);
        await _userRepository.UpdateAsync(user);

        try
        {
            await _emailService.SendPasswordResetCodeAsync(user.Email, user.FirstName, code);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password reset email to {Email}", user.Email);
        }

        return new GenericResponseDTO { Success = true, Message = "If this email is registered, a reset code has been sent." };
    }

    public async Task<GenericResponseDTO> ResetPasswordAsync(ResetPasswordRequestDTO request)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            return new GenericResponseDTO { Success = false, Message = "Passwords do not match." };
        }

        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || user.PasswordResetCode == null || user.PasswordResetCodeExpiry == null)
        {
            return new GenericResponseDTO { Success = false, Message = "Invalid or expired reset code." };
        }

        if (user.PasswordResetCode != request.Code || DateTime.UtcNow > user.PasswordResetCodeExpiry)
        {
            return new GenericResponseDTO { Success = false, Message = "Invalid or expired reset code." };
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetCode = null;
        user.PasswordResetCodeExpiry = null;
        await _userRepository.UpdateAsync(user);

        return new GenericResponseDTO { Success = true, Message = "Password reset successfully. You can now log in." };
    }

    private static UserDTO MapToUserDTO(User user)
    {
        return new UserDTO
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            InviteCode = user.InviteCode,
            AssignedDoctorId = user.AssignedDoctorId
        };
    }

    private static string GenerateInviteCode()
    {
        var random = new Random();
        var code = new string(Enumerable.Range(0, ScanConstants.InviteCodeLength)
            .Select(_ => ScanConstants.InviteCodeChars[random.Next(ScanConstants.InviteCodeChars.Length)])
            .ToArray());
        return $"{ScanConstants.DoctorCodePrefix}{code}";
    }
}
