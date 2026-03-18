using Microsoft.EntityFrameworkCore;
using NeuroScan.Domain.Entities;
using NeuroScan.Domain.IRepositories;
using NeuroScan.Infrastructure.Context;

namespace NeuroScan.Infrastructure.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly ApplicationDbContext _context;

    public PatientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Patient?> GetByIdAsync(Guid id)
    {
        return await _context.Patients
            .Where(p => p.DeletedAt == null)
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Patient>> GetAllAsync()
    {
        return await _context.Patients
            .Where(p => p.DeletedAt == null)
            .Include(p => p.CreatedBy)
            .ToListAsync();
    }

    public async Task<IEnumerable<Patient>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Patients
            .Where(p => p.DeletedAt == null && p.CreatedByUserId == userId)
            .Include(p => p.CreatedBy)
            .ToListAsync();
    }

    public async Task<Patient?> GetByMedicalRecordNumberAsync(string mrn)
    {
        return await _context.Patients
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.MedicalRecordNumber == mrn);
    }

    public async Task<Patient?> GetByPatientUserIdAsync(Guid userId)
    {
        return await _context.Patients
            .Where(p => p.DeletedAt == null && p.UserId == userId)
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
