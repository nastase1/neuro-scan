using Microsoft.EntityFrameworkCore;
using NeuroScan.Domain.Entities;
using NeuroScan.Domain.IRepositories;
using NeuroScan.Infrastructure.Context;

namespace NeuroScan.Infrastructure.Repositories;

public class AnalysisResultRepository : IAnalysisResultRepository
{
    private readonly ApplicationDbContext _context;

    public AnalysisResultRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AnalysisResult?> GetByMriScanIdAsync(Guid scanId)
    {
        return await _context.AnalysisResults
            .Where(a => a.DeletedAt == null)
            .FirstOrDefaultAsync(a => a.MriScanId == scanId);
    }

    public async Task AddAsync(AnalysisResult result)
    {
        await _context.AnalysisResults.AddAsync(result);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AnalysisResult result)
    {
        _context.AnalysisResults.Update(result);
        await _context.SaveChangesAsync();
    }
}
