using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;

namespace DerasaX.Application.Services.Operations
{
    public class SupportService : OperationsServiceBase, ISupportService
    {
        public SupportService(IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit) : base(uow, tenant, audit) { }

        public async Task<ApiResponse<SupportRequestDto>> CreateAsync(CreateSupportRequestDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var caller = RequireUser();
            if (string.IsNullOrWhiteSpace(dto.Message)) throw new BadRequestException("Message is required.");
            var request = new SupportRequest
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, UserId = caller, Type = dto.Type,
                Status = RequestStatus.Pending, Message = dto.Message, CreatedAt = DateTime.UtcNow
            };
            await UnitOfWork.Repository<SupportRequest, string>().AddAsync(request);
            await Audit.StageAsync(AuditActionType.Create, nameof(SupportRequest), request.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(request), 201, "Support request created.");
        }

        public async Task<PaginationResponse<IEnumerable<SupportRequestDto>>> ListAsync(SupportParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            var caller = RequireUser();
            // SchoolAdmin sees all tenant requests; everyone else sees only their own.
            Expression<Func<SupportRequest, bool>> criteria = IsSchoolAdmin
                ? r => !p.Status.HasValue || r.Status == p.Status.Value
                : r => r.UserId == caller && (!p.Status.HasValue || r.Status == p.Status.Value);

            var repo = UnitOfWork.Repository<SupportRequest, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<SupportRequest, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<SupportRequest, string>(criteria, r => r.CreatedAt, p.PageNumber, p.PageSize, descending: true));
            return new PaginationResponse<IEnumerable<SupportRequestDto>>(items.Select(Map).ToList(), total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Support requests retrieved." };
        }

        public async Task<ApiResponse<SupportRequestDto>> GetAsync(string id, CancellationToken ct = default)
        {
            var caller = RequireUser();
            var request = await UnitOfWork.Repository<SupportRequest, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<SupportRequest, string>(r => r.Id == id)) ?? throw new NotFoundException("Support request not found.");
            if (!IsSchoolAdmin && request.UserId != caller) throw new NotFoundException("Support request not found.");
            return Ok(Map(request));
        }

        public async Task<ApiResponse<SupportRequestDto>> RespondAsync(string id, RespondSupportDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (!IsSchoolAdmin) throw new ForbiddenException("Only a school administrator may respond to support requests.");
            if (string.IsNullOrWhiteSpace(dto.ResponseMessage)) throw new BadRequestException("ResponseMessage is required.");
            var request = await UnitOfWork.Repository<SupportRequest, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<SupportRequest, string>(r => r.Id == id)) ?? throw new NotFoundException("Support request not found.");

            request.ResponseMessage = dto.ResponseMessage;
            request.Status = dto.Status;
            request.RespondedAt = DateTime.UtcNow;
            UnitOfWork.Repository<SupportRequest, string>().Update(request);
            await Audit.StageAsync(AuditActionType.Update, nameof(SupportRequest), request.Id, $"{{\"status\":\"{dto.Status}\"}}", ct);
            await NotificationStaging.StageAsync(UnitOfWork, tenantId, request.UserId,
                "Support request updated", "Your support request received a response.",
                NotificationCategory.General, NotificationType.System, actorUserId: Tenant.UserId,
                metadataJson: $"{{\"supportRequestId\":\"{request.Id}\"}}", ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(request), 200, "Support request updated.");
        }

        private static SupportRequestDto Map(SupportRequest r) => new()
        {
            Id = r.Id, TenantId = r.TenantId, UserId = r.UserId, Type = r.Type, Status = r.Status,
            Message = r.Message, ResponseMessage = r.ResponseMessage, CreatedAt = r.CreatedAt, RespondedAt = r.RespondedAt
        };
    }
}
