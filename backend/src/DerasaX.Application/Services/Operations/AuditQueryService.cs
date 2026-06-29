using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;

namespace DerasaX.Application.Services.Operations
{
    /// <summary>
    /// Authorized, paginated, filterable audit queries. SchoolAdmin reads its own tenant's audit
    /// trail (the global tenant query filter scopes the rows; another tenant's audit data is never
    /// returned). SystemAdmin (platform scope) reads the full platform audit trail. Sensitive
    /// payloads are not exposed — only the structured audit metadata is returned.
    /// </summary>
    public class AuditQueryService : OperationsServiceBase, IAuditQueryService
    {
        public AuditQueryService(IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit) : base(uow, tenant, audit) { }

        public async Task<PaginationResponse<IEnumerable<AuditLogDto>>> QueryAsync(AuditParameters p, bool platformScope, CancellationToken ct = default)
        {
            if (platformScope && !IsSystemAdmin)
                throw new ForbiddenException("Only a platform administrator may read the platform audit trail.");
            if (!platformScope) RequireTenant();

            DateTime? from = p.From.HasValue ? AsUtc(p.From.Value) : null;
            DateTime? to = p.To.HasValue ? AsUtc(p.To.Value) : null;
            if (from.HasValue && to.HasValue && to < from) throw new BadRequestException("'To' must be on or after 'From'.");

            Expression<Func<AuditLog, bool>> criteria = a =>
                (string.IsNullOrEmpty(p.EntityType) || a.EntityType == p.EntityType) &&
                (!p.Action.HasValue || a.Action == p.Action.Value) &&
                (string.IsNullOrEmpty(p.ActorUserId) || a.ActorUserId == p.ActorUserId) &&
                (string.IsNullOrEmpty(p.CorrelationId) || a.CorrelationId == p.CorrelationId) &&
                (from == null || a.OccurredAt >= from) && (to == null || a.OccurredAt <= to);

            // The DbContext global filter restricts SchoolAdmin to its own tenant automatically;
            // SystemAdmin runs in platform scope so the filter is bypassed (full platform trail).
            var repo = UnitOfWork.Repository<AuditLog, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<AuditLog, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<AuditLog, string>(criteria, a => a.OccurredAt, p.PageNumber, p.PageSize, descending: true));
            var dto = items.Select(a => new AuditLogDto
            {
                Id = a.Id, TenantId = a.TenantId, ActorUserId = a.ActorUserId, Action = a.Action,
                EntityType = a.EntityType, EntityId = a.EntityId, CorrelationId = a.CorrelationId, OccurredAt = a.OccurredAt
            }).ToList();
            return new PaginationResponse<IEnumerable<AuditLogDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Audit records retrieved." };
        }

        private static DateTime AsUtc(DateTime v) => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc);
    }
}
