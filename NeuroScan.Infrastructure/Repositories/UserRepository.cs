using Microsoft.EntityFrameworkCore;
using NeuroScan.Domain.Entities;
using NeuroScan.Domain.IRepositories;
using NeuroScan.Infrastructure.Context;

namespace NeuroScan.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await ActiveEntities.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await ActiveEntities.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> GetByInviteCodeAsync(string inviteCode)
    {
        return await ActiveEntities
            .Where(u => u.Role == UserRole.Doctor)
            .FirstOrDefaultAsync(u => u.InviteCode == inviteCode);
    }

    public async Task<IEnumerable<User>> GetAssignedUsersAsync(Guid doctorId)
    {
        return await ActiveEntities
            .Where(u => u.AssignedDoctorId == doctorId)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await ActiveEntities.ToListAsync();
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role)
    {
        return await ActiveEntities
            .Where(u => u.Role == role)
            .ToListAsync();
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}
