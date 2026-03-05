using NeuroScan.Domain.Entities;

namespace NeuroScan.Domain.IRepositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByInviteCodeAsync(string inviteCode);
    Task<IEnumerable<User>> GetAllAsync();
    Task<IEnumerable<User>> GetByRoleAsync(UserRole role);
    Task<IEnumerable<User>> GetAssignedUsersAsync(Guid doctorId);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
