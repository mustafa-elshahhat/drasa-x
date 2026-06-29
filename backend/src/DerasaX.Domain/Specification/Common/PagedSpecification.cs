using System;
using System.Linq.Expressions;
using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Specification.Common
{
    /// <summary>
    /// Generic paged + ordered specification for tenant-scoped list endpoints. Tenant
    /// isolation is enforced by the DbContext global query filter, so the supplied
    /// <paramref name="criteria"/> only needs to express the endpoint-specific filter.
    /// Paging bounds are applied by the caller (see <c>PaginationParameters</c>) so the
    /// result set is always bounded.
    /// </summary>
    public class PagedSpecification<TEntity, TKey> : BaseSpecification<TEntity, TKey>
        where TEntity : BaseEntity<TKey>
    {
        public PagedSpecification(
            Expression<Func<TEntity, bool>> criteria,
            Expression<Func<TEntity, object>> orderBy,
            int pageNumber,
            int pageSize,
            bool descending = false,
            params Expression<Func<TEntity, object>>[] includes)
            : base(criteria)
        {
            foreach (var include in includes)
                AddInclude(include);

            if (descending)
                AddOrderByDescending(orderBy);
            else
                AddOrderBy(orderBy);

            ApplyPaging(pageNumber, pageSize);
        }
    }

    /// <summary>
    /// Unpaged criteria specification — used for single-entity lookups and for the
    /// total-count companion query of a paged list (same filter, no Skip/Take).
    /// </summary>
    public class CriteriaSpecification<TEntity, TKey> : BaseSpecification<TEntity, TKey>
        where TEntity : BaseEntity<TKey>
    {
        public CriteriaSpecification(
            Expression<Func<TEntity, bool>> criteria,
            params Expression<Func<TEntity, object>>[] includes)
            : base(criteria)
        {
            foreach (var include in includes)
                AddInclude(include);
        }
    }
}
