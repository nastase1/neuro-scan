using Microsoft.EntityFrameworkCore;
using NeuroScan.Domain.Entities;
using NeuroScan.Domain.IRepositories;
using NeuroScan.Infrastructure.Context;

namespace NeuroScan.Infrastructure.Repositories;

public class AnalysisResultRepository : BaseRepository<AnalysisResult>, IAnalysisResultRepository
{
    public AnalysisResultRepository(ApplicationDbContext context) : base(context) { }

    public async Task<AnalysisResult?> GetByMriScanIdAsync(Guid scanId)
    {
        return await ActiveEntities.FirstOrDefaultAsync(a => a.MriScanId == scanId);
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
