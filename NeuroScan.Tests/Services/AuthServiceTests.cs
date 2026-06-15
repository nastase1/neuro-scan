using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NeuroScan.Application.IServices;
using NeuroScan.Application.DTOs;
using NeuroScan.Application.Services;
using NeuroScan.Domain.Entities;
using NeuroScan.Domain.IRepositories;

namespace NeuroScan.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo;
    private readonly Mock<IPatientRepository> _patientRepo;
    private readonly Mock<IJwtTokenService> _jwtTokenService;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<ILogger<AuthService>> _logger;
    private readonly Mock<IConfiguration> _configuration;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _userRepo = new Mock<IUserRepository>();
        _patientRepo = new Mock<IPatientRepository>();
        _jwtTokenService = new Mock<IJwtTokenService>();
        _emailService = new Mock<IEmailService>();
        _logger = new Mock<ILogger<AuthService>>();
        _configuration = new Mock<IConfiguration>();
        _configuration.Setup(c => c["Google:ClientId"]).Returns("test-google-client-id");

        _sut = new AuthService(
            _userRepo.Object,
            _patientRepo.Object,
            _jwtTokenService.Object,
            _emailService.Object,
            _logger.Object,
            _configuration.Object);
    }

    // ── RegisterAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ReturnsFailure()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("existing@test.com"))
            .ReturnsAsync(new User { FirstName = "X", LastName = "X", Email = "existing@test.com", PasswordHash = "h", Role = UserRole.StandardUser });

        var result = await _sut.RegisterAsync(new RegisterRequestDTO
        {
            FirstName = "A", LastName = "B", Email = "existing@test.com", Password = "pass"
        });

        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message);
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_CreatesUserAndReturnsToken()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _jwtTokenService.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("test-token");
        _emailService.Setup(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _sut.RegisterAsync(new RegisterRequestDTO
        {
            FirstName = "Jane", LastName = "Doe", Email = "new@test.com", Password = "securePass"
        });

        Assert.True(result.Success);
        Assert.Equal("test-token", result.Token);
        Assert.NotNull(result.User);
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_DoctorRole_SetsInviteCode()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _jwtTokenService.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("token");

        User? savedUser = null;
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => savedUser = u)
            .Returns(Task.CompletedTask);

        await _sut.RegisterAsync(new RegisterRequestDTO
        {
            FirstName = "Dr", LastName = "Smith", Email = "doc@test.com",
            Password = "pass", Role = UserRole.Doctor
        });

        Assert.NotNull(savedUser!.InviteCode);
        Assert.StartsWith("DR-", savedUser.InviteCode);
    }

    [Fact]
    public async Task RegisterAsync_WithValidInviteCode_AssignsDoctorAndCreatesPatient()
    {
        var doctorId = Guid.NewGuid();
        var doctor = new User { Id = doctorId, FirstName = "Doc", LastName = "Smith", Email = "doc@test.com", PasswordHash = "h", Role = UserRole.Doctor };

        _userRepo.Setup(r => r.GetByEmailAsync("patient@test.com")).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.GetByInviteCodeAsync("DR-VALIDCOD")).ReturnsAsync(doctor);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _patientRepo.Setup(r => r.AddAsync(It.IsAny<Patient>())).Returns(Task.CompletedTask);
        _jwtTokenService.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("token");

        var result = await _sut.RegisterAsync(new RegisterRequestDTO
        {
            FirstName = "Pat", LastName = "ient", Email = "patient@test.com",
            Password = "pass", Role = UserRole.StandardUser, InviteCode = "DR-VALIDCOD"
        });

        Assert.True(result.Success);
        _patientRepo.Verify(r => r.AddAsync(It.IsAny<Patient>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidInviteCode_ReturnsFailure()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.GetByInviteCodeAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await _sut.RegisterAsync(new RegisterRequestDTO
        {
            FirstName = "P", LastName = "P", Email = "p@test.com",
            Password = "pass", Role = UserRole.StandardUser, InviteCode = "INVALID"
        });

        Assert.False(result.Success);
        Assert.Contains("Invalid invite code", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailServiceFails_StillSucceeds()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _jwtTokenService.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("token");
        _emailService.Setup(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP error"));

        var result = await _sut.RegisterAsync(new RegisterRequestDTO
        {
            FirstName = "A", LastName = "B", Email = "a@test.com", Password = "pass"
        });

        Assert.True(result.Success);
    }

    // ── LoginAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessWithToken()
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("correctPass");
        var user = new User { Id = Guid.NewGuid(), FirstName = "A", LastName = "B", Email = "test@test.com", PasswordHash = hashedPassword, Role = UserRole.StandardUser };

        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        _jwtTokenService.Setup(j => j.GenerateToken(user)).Returns("jwt-token");

        var result = await _sut.LoginAsync(new LoginRequestDTO { Email = "test@test.com", Password = "correctPass" });

        Assert.True(result.Success);
        Assert.Equal("jwt-token", result.Token);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsFailure()
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("correctPass");
        var user = new User { Id = Guid.NewGuid(), FirstName = "A", LastName = "B", Email = "test@test.com", PasswordHash = hashedPassword, Role = UserRole.StandardUser };

        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequestDTO { Email = "test@test.com", Password = "wrongPass" });

        Assert.False(result.Success);
        Assert.Contains("Invalid email or password", result.Message);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ReturnsFailure()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await _sut.LoginAsync(new LoginRequestDTO { Email = "nobody@test.com", Password = "pass" });

        Assert.False(result.Success);
    }

    // ── ForgotPasswordAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPasswordAsync_WithUnknownEmail_ReturnsSilentSuccess()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequestDTO { Email = "ghost@test.com" });

        Assert.True(result.Success);
        _emailService.Verify(e => e.SendPasswordResetCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithKnownEmail_SetsResetCodeAndSendsEmail()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "A", LastName = "B", Email = "known@test.com", PasswordHash = "h", Role = UserRole.StandardUser };

        _userRepo.Setup(r => r.GetByEmailAsync("known@test.com")).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _emailService.Setup(e => e.SendPasswordResetCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        await _sut.ForgotPasswordAsync(new ForgotPasswordRequestDTO { Email = "known@test.com" });

        _userRepo.Verify(r => r.UpdateAsync(It.Is<User>(u => u.PasswordResetCode != null)), Times.Once);
        _emailService.Verify(e => e.SendPasswordResetCodeAsync("known@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    // ── ResetPasswordAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPasswordAsync_WithMismatchedPasswords_ReturnsFailure()
    {
        var result = await _sut.ResetPasswordAsync(new ResetPasswordRequestDTO
        {
            Email = "a@b.com", Code = "123456", NewPassword = "pass1", ConfirmPassword = "pass2"
        });

        Assert.False(result.Success);
        Assert.Contains("do not match", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredCode_ReturnsFailure()
    {
        var user = new User
        {
            FirstName = "A", LastName = "B", Email = "a@test.com", PasswordHash = "h", Role = UserRole.StandardUser,
            PasswordResetCode = "123456",
            PasswordResetCodeExpiry = DateTime.UtcNow.AddMinutes(-1)
        };

        _userRepo.Setup(r => r.GetByEmailAsync("a@test.com")).ReturnsAsync(user);

        var result = await _sut.ResetPasswordAsync(new ResetPasswordRequestDTO
        {
            Email = "a@test.com", Code = "123456", NewPassword = "newPass", ConfirmPassword = "newPass"
        });

        Assert.False(result.Success);
        Assert.Contains("expired", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidCode_ResetsPassword()
    {
        var user = new User
        {
            FirstName = "A", LastName = "B", Email = "a@test.com", PasswordHash = "oldHash", Role = UserRole.StandardUser,
            PasswordResetCode = "123456",
            PasswordResetCodeExpiry = DateTime.UtcNow.AddMinutes(10)
        };

        _userRepo.Setup(r => r.GetByEmailAsync("a@test.com")).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var result = await _sut.ResetPasswordAsync(new ResetPasswordRequestDTO
        {
            Email = "a@test.com", Code = "123456", NewPassword = "newPass", ConfirmPassword = "newPass"
        });

        Assert.True(result.Success);
        _userRepo.Verify(r => r.UpdateAsync(It.Is<User>(u => u.PasswordResetCode == null)), Times.Once);
    }

    // ── GetMyInviteCodeAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetMyInviteCodeAsync_ForDoctorWithExistingCode_ReturnsCode()
    {
        var doctorId = Guid.NewGuid();
        var doctor = new User { Id = doctorId, FirstName = "D", LastName = "D", Email = "d@d.com", PasswordHash = "h", Role = UserRole.Doctor, InviteCode = "DR-EXISTING" };

        _userRepo.Setup(r => r.GetByIdAsync(doctorId)).ReturnsAsync(doctor);

        var code = await _sut.GetMyInviteCodeAsync(doctorId);

        Assert.Equal("DR-EXISTING", code);
    }

    [Fact]
    public async Task GetMyInviteCodeAsync_ForNonDoctor_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, FirstName = "U", LastName = "U", Email = "u@u.com", PasswordHash = "h", Role = UserRole.StandardUser };

        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var code = await _sut.GetMyInviteCodeAsync(userId);

        Assert.Null(code);
    }
}
