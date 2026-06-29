using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Domain.Interfaces.RepositoriesInterfaces
{
    public interface IGenericRepository<TEntity,TKey> where TEntity:BaseEntity<TKey>
    {
        Task<TEntity> GetByIdAsync(TKey id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task AddAsync(TEntity entity);
        Task AddRangeAsync(IEnumerable<TEntity> entities);
        void Update(TEntity entity);
        void Delete(TEntity entity);
        //specification methods
        // Phase 22 Step 8 (PERF-01): read-only callers may opt into AsNoTracking to skip change-tracking
        // on list reads that only project to DTOs. Defaults to false so existing (incl. write) callers are
        // unchanged. Never pass true on a path that mutates + saves the returned entities.
        Task<IEnumerable<TEntity>> GetAllWithSpecAsync(ISpecification<TEntity, TKey> specification, bool asNoTracking = false);
        Task<TEntity> GetByIdWithSpecAsync(ISpecification<TEntity, TKey> specification);
        Task<int> CountAsync(ISpecification<TEntity, TKey> specification);
    }
}
