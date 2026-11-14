using ClinicaApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicaApp.Repositories;

public class EfRepository<TEntity>(ClinicaDbContext context) : IRepository<TEntity>
    where TEntity : class
{
    protected ClinicaDbContext Context { get; } = context;

    public async Task<IEnumerable<TEntity>> GetAllAsync() =>
        await Context.Set<TEntity>().AsNoTracking().ToListAsync();

    public async Task<TEntity?> GetByIdAsync(int id) =>
        await Context.Set<TEntity>().FindAsync(id);

    public async Task AddAsync(TEntity entity) =>
        await Context.Set<TEntity>().AddAsync(entity);

    public void Update(TEntity entity) =>
        Context.Set<TEntity>().Update(entity);

    public void Delete(TEntity entity) =>
        Context.Set<TEntity>().Remove(entity);
}
