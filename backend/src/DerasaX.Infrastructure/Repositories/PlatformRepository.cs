using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Domain.Interfaces;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Infrastructure.Repositories
{
    /// <inheritdoc />
    public class PlatformRepository<TEntity> : IPlatformRepository<TEntity> where TEntity : class
    {
        private readonly DerasaXDbContext _context;
        public PlatformRepository(DerasaXDbContext context) => _context = context;

        private DbSet<TEntity> Set => _context.Set<TEntity>();

        public Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
            Set.Where(predicate).ToListAsync(ct);

        public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
            Set.FirstOrDefaultAsync(predicate, ct);

        public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
            Set.CountAsync(predicate, ct);

        public async Task AddAsync(TEntity entity, CancellationToken ct = default) =>
            await Set.AddAsync(entity, ct);

        public void Update(TEntity entity) => Set.Update(entity);
    }
}
