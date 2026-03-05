using NeuroScan.Application.IServices;
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

    public AuthService(IUserRepository userRepository, IPatientRepository patientRepository, IJwtTokenService jwtTokenService, IEmailService emailService)
    {
        _userRepository = userRepository;
        _patientRepository = patientRepository;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
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

        // Doctors get a unique invite code on registration
        if (request.Role == UserRole.Doctor)
        {
            user.InviteCode = GenerateInviteCode();
        }

        // Standard users can optionally link to a doctor via invite code
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

        // Auto-create a Patient record for this user under the assigned doctor
        if (assignedDoctorId.HasValue)
        {
            var mrn = "USR-" + user.Id.ToString("N")[..8].ToUpper();
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = DateTime.MinValue,
                MedicalRecordNumber = mrn,
                CreatedByUserId = assignedDoctorId.Value,
                CreatedAt = DateTime.UtcNow
            };
            await _patientRepository.AddAsync(patient);
        }

        // Send welcome email (fire-and-forget; don't fail registration if email fails)
        try { await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName); } catch { }

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

    public async Task<string?> GetMyInviteCodeAsync(Guid doctorId)
    {
        var doctor = await _userRepository.GetByIdAsync(doctorId);
        if (doctor == null || doctor.Role != UserRole.Doctor) return null;

        // Generate one if missing (for existing doctors)
        if (string.IsNullOrEmpty(doctor.InviteCode))
        {
            doctor.InviteCode = GenerateInviteCode();
            await _userRepository.UpdateAsync(doctor);
        }

        return doctor.InviteCode;
    }

    public async Task<GenericResponseDTO> ForgotPasswordAsync(ForgotPasswordRequestDTO request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
        {
            // Return success anyway to avoid email enumeration
            return new GenericResponseDTO { Success = true, Message = "If this email is registered, a reset code has been sent." };
        }

        var code = new Random().Next(100000, 999999).ToString();
        user.PasswordResetCode = code;
        user.PasswordResetCodeExpiry = DateTime.UtcNow.AddMinutes(15);
        await _userRepository.UpdateAsync(user);

        try { await _emailService.SendPasswordResetCodeAsync(user.Email, user.FirstName, code); } catch { }

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
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        var code = new string(Enumerable.Range(0, 8).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        return $"DR-{code}";
    }
}
