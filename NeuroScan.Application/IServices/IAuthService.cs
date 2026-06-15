using NeuroScan.Domain.Entities;

using NeuroScan.Application.DTOs;

namespace NeuroScan.Application.IServices;

public interface IAuthService
{
    Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO request);
    Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request);
    Task<AuthResponseDTO> GoogleLoginAsync(GoogleAuthRequestDTO request);
    Task<User?> GetCurrentUserAsync(Guid userId);
    Task<GenericResponseDTO> ForgotPasswordAsync(ForgotPasswordRequestDTO request);
    Task<GenericResponseDTO> ResetPasswordAsync(ResetPasswordRequestDTO request);
    Task<string?> GetMyInviteCodeAsync(Guid doctorId);
    Task<UpdateProfileResponseDTO> UpdateProfileAsync(Guid userId, UpdateProfileRequestDTO request);
}
