using NeuroScan.Domain.Entities;
using NeuroScan.Infrastructure.Context;

namespace NeuroScan.Infrastructure.Repositories;

public abstract class BaseRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;

    protected BaseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    protected IQueryable<T> ActiveEntities => _context.Set<T>().Where(e => e.DeletedAt == null);
}
