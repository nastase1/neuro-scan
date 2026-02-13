using NeuroScan.Domain.Entities;

namespace NeuroScan.Application.IServices;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
