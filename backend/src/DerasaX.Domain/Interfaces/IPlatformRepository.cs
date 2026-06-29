using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace DerasaX.Domain.Interfaces
{
    /// <summary>
    /// Minimal repository for PLATFORM-owned entities (those deriving from
    /// <c>PlatformEntity</c>): global catalogs and platform configuration that are NOT
    /// tenant-scoped, so they must not be forced through the tenant-owned
    /// <see cref="IGenericRepository{TEntity,TKey}"/> (which constrains to a tenant
    /// <c>BaseEntity</c> and applies the tenant query filter). The implementation shares the
    /// same scoped DbContext as the unit of work, so writes staged here commit together with
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> in a single transaction.
    /// </summary>
    public interface IPlatformRepository<TEntity> where TEntity : class
    {
        Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
        Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
        Task AddAsync(TEntity entity, CancellationToken ct = default);
        void Update(TEntity entity);
    }
}
