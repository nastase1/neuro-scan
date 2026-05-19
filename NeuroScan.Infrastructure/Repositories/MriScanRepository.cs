using Microsoft.EntityFrameworkCore;
using NeuroScan.Domain.Entities;
using NeuroScan.Domain.IRepositories;
using NeuroScan.Infrastructure.Context;

namespace NeuroScan.Infrastructure.Repositories;

public class MriScanRepository : BaseRepository<MriScan>, IMriScanRepository
{
    public MriScanRepository(ApplicationDbContext context) : base(context) { }

    public async Task<MriScan?> GetByIdAsync(Guid id)
    {
        return await ActiveEntities
            .Include(s => s.Patient)
            .Include(s => s.ReviewedByDoctor)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<MriScan>> GetByPatientIdAsync(Guid patientId)
    {
        return await ActiveEntities
            .Where(s => s.PatientId == patientId)
            .Include(s => s.Patient)
            .Include(s => s.AnalysisResult)
            .OrderByDescending(s => s.UploadDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<MriScan>> GetPendingReviewScansAsync()
    {
        return await ActiveEntities
            .Where(s => s.Status == ScanStatus.Analyzed && s.ReviewedByDoctorId == null)
            .Include(s => s.Patient)
            .Include(s => s.AnalysisResult)
            .OrderBy(s => s.UploadDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<MriScan>> GetByStatusAsync(ScanStatus status)
    {
        return await ActiveEntities
            .Where(s => s.Status == status)
            .Include(s => s.Patient)
            .Include(s => s.AnalysisResult)
            .OrderByDescending(s => s.UploadDate)
            .ToListAsync();
    }

    public async Task AddAsync(MriScan scan)
    {
        await _context.MriScans.AddAsync(scan);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(MriScan scan)
    {
        _context.MriScans.Update(scan);
        await _context.SaveChangesAsync();
    }
}
