using Microsoft.EntityFrameworkCore;
using NeuroScan.Domain.Entities;
using NeuroScan.Domain.IRepositories;
using NeuroScan.Infrastructure.Context;

namespace NeuroScan.Infrastructure.Repositories;

public class PatientRepository : BaseRepository<Patient>, IPatientRepository
{
    public PatientRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Patient?> GetByIdAsync(Guid id)
    {
        return await ActiveEntities
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Patient>> GetAllAsync()
    {
        return await ActiveEntities
            .Include(p => p.CreatedBy)
            .ToListAsync();
    }

    public async Task<IEnumerable<Patient>> GetByUserIdAsync(Guid userId)
    {
        return await ActiveEntities
            .Where(p => p.CreatedByUserId == userId)
            .Include(p => p.CreatedBy)
            .ToListAsync();
    }

    public async Task<Patient?> GetByMedicalRecordNumberAsync(string mrn)
    {
        return await ActiveEntities
            .FirstOrDefaultAsync(p => p.MedicalRecordNumber == mrn);
    }

    public async Task<Patient?> GetByPatientUserIdAsync(Guid userId)
    {
        return await ActiveEntities
            .Where(p => p.UserId == userId)
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(Patient patient)
    {
        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Patient patient)
    {
        _context.Patients.Update(patient);
        await _context.SaveChangesAsync();
    }
}
