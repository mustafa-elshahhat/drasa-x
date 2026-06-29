using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.CommunicationDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Communication;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;

namespace DerasaX.Application.Services.Communication
{
    public class ParentRequestService : CommunicationServiceBase, IParentRequestService
    {
        public ParentRequestService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit,
            UserManager<ApplicationUser> users) : base(unitOfWork, tenant, audit, users) { }

        public async Task<ApiResponse<ParentRequestDto>> CreateAsync(CreateParentRequestDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var parentId = RequireUser();
            if (!IsParent) throw new ForbiddenException("Only a parent may create a request.");
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Body))
                throw new BadRequestException("Title and Body are required.");

            // The request must concern a linked child with the appropriate permission.
            var link = (await UnitOfWork.Repository<ParentStudentRelationship, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<ParentStudentRelationship, string>(r =>
                    r.ParentId == parentId && r.StudentId == dto.StudentId && r.IsActive))).FirstOrDefault()
                ?? throw new NotFoundException("Linked child not found.");
            if (dto.Type == ParentRequestType.Document && !link.CanRequestDocuments)
                throw new ForbiddenException("You do not have permission to request documents for this child.");

            var request = new ParentRequest
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                ParentId = parentId,
                StudentId = dto.StudentId,
                Type = dto.Type,
                Status = ParentRequestStatus.Open,
                Title = dto.Title,
                Body = dto.Body,
                RequestedAt = DateTime.UtcNow
            };
            await UnitOfWork.Repository<ParentRequest, string>().AddAsync(request);
            await Audit.StageAsync(AuditActionType.Create, nameof(ParentRequest), request.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(request, new List<ParentRequestResponse>()), 201, "Request created.");
        }

        public async Task<PaginationResponse<IEnumerable<ParentRequestDto>>> ListAsync(ParentRequestParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            var caller = RequireUser();
            // A parent sees only their own requests; SchoolAdmin sees the whole tenant.
            Expression<Func<ParentRequest, bool>> criteria;
            if (IsParent) criteria = r => r.ParentId == caller && (!p.Status.HasValue || r.Status == p.Status.Value);
            else if (IsSchoolAdmin) criteria = r => (!p.Status.HasValue || r.Status == p.Status.Value);
            else throw new ForbiddenException("You are not permitted to view parent requests.");

            var repo = UnitOfWork.Repository<ParentRequest, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<ParentRequest, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<ParentRequest, string>(criteria, r => r.RequestedAt, p.PageNumber, p.PageSize, descending: true));
            var dto = items.Select(r => Map(r, new List<ParentRequestResponse>())).ToList();
            return new PaginationResponse<IEnumerable<ParentRequestDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Requests retrieved." };
        }

        public async Task<ApiResponse<ParentRequestDto>> GetAsync(string id, CancellationToken ct = default)
        {
            var request = await LoadAuthorizedAsync(id);
            var responses = await LoadResponses(request.Id);
            return Ok(Map(request, responses), 200, "Request retrieved.");
        }

        public async Task<ApiResponse<ParentRequestResponseDto>> RespondAsync(string id, RespondParentRequestDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var responder = RequireUser();
            if (!IsSchoolAdmin) throw new ForbiddenException("Only school staff may respond to a request.");
            if (string.IsNullOrWhiteSpace(dto.Body)) throw new BadRequestException("Response body is required.");

            var request = await UnitOfWork.Repository<ParentRequest, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<ParentRequest, string>(r => r.Id == id))
                ?? throw new NotFoundException("Request not found.");

            var response = new ParentRequestResponse
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, ParentRequestId = request.Id,
                ResponderId = responder, Body = dto.Body, RespondedAt = DateTime.UtcNow
            };
            await UnitOfWork.Repository<ParentRequestResponse, string>().AddAsync(response);
            if (request.Status == ParentRequestStatus.Open)
            {
                request.Status = ParentRequestStatus.InProgress;
                UnitOfWork.Repository<ParentRequest, string>().Update(request);
            }
            await Audit.StageAsync(AuditActionType.Update, nameof(ParentRequest), request.Id, "{\"action\":\"respond\"}", ct);
            await StageNotificationAsync(tenantId, request.ParentId, "Response to your request",
                "Your request received a response.", NotificationCategory.General);
            await UnitOfWork.SaveChangesAsync(ct);

            return Ok(new ParentRequestResponseDto { Id = response.Id, ResponderId = responder, Body = response.Body, RespondedAt = response.RespondedAt },
                201, "Response added.");
        }

        public async Task<ApiResponse<ParentRequestDto>> TransitionAsync(string id, TransitionParentRequestDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (!IsSchoolAdmin) throw new ForbiddenException("Only school staff may change a request's status.");

            var request = await UnitOfWork.Repository<ParentRequest, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<ParentRequest, string>(r => r.Id == id))
                ?? throw new NotFoundException("Request not found.");

            if (!IsValidTransition(request.Status, dto.Status))
                throw new ConflictException($"Cannot transition a request from {request.Status} to {dto.Status}.");

            request.Status = dto.Status;
            if (dto.Status is ParentRequestStatus.Resolved or ParentRequestStatus.Rejected or ParentRequestStatus.Closed)
                request.ResolvedAt = DateTime.UtcNow;
            UnitOfWork.Repository<ParentRequest, string>().Update(request);
            await Audit.StageAsync(AuditActionType.Update, nameof(ParentRequest), request.Id, $"{{\"status\":\"{dto.Status}\"}}", ct);
            await StageNotificationAsync(tenantId, request.ParentId, "Request status updated",
                $"Your request is now {dto.Status}.", NotificationCategory.General);
            await UnitOfWork.SaveChangesAsync(ct);

            var responses = await LoadResponses(request.Id);
            return Ok(Map(request, responses), 200, "Request updated.");
        }

        // ---- Phase 16: sensitive document attachments ----

        public async Task<ApiResponse<bool>> AttachRequestDocumentAsync(string id, string fileRecordId, CancellationToken ct = default)
        {
            var caller = RequireUser();
            if (!IsParent) throw new ForbiddenException("Only the requesting parent may attach a document to a request.");
            var request = await UnitOfWork.Repository<ParentRequest, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<ParentRequest, string>(r => r.Id == id))
                ?? throw new NotFoundException("Request not found.");
            if (request.ParentId != caller) throw new NotFoundException("Request not found.");

            request.FileRecordId = fileRecordId;
            UnitOfWork.Repository<ParentRequest, string>().Update(request);
            await Audit.StageAsync(AuditActionType.Update, nameof(ParentRequest), request.Id, "{\"action\":\"attach-document\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(true, 200, "Document attached.");
        }

        public async Task<ApiResponse<ParentRequestResponseDto>> AttachResponseDocumentAsync(string id, string fileRecordId, string? body, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var responder = RequireUser();
            if (!IsSchoolAdmin) throw new ForbiddenException("Only school staff may attach a response document.");

            var request = await UnitOfWork.Repository<ParentRequest, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<ParentRequest, string>(r => r.Id == id))
                ?? throw new NotFoundException("Request not found.");

            var response = new ParentRequestResponse
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, ParentRequestId = request.Id,
                ResponderId = responder, Body = string.IsNullOrWhiteSpace(body) ? "(document attached)" : body,
                RespondedAt = DateTime.UtcNow, FileRecordId = fileRecordId
            };
            await UnitOfWork.Repository<ParentRequestResponse, string>().AddAsync(response);
            if (request.Status == ParentRequestStatus.Open)
            {
                request.Status = ParentRequestStatus.InProgress;
                UnitOfWork.Repository<ParentRequest, string>().Update(request);
            }
            await Audit.StageAsync(AuditActionType.Update, nameof(ParentRequest), request.Id, "{\"action\":\"respond-document\"}", ct);
            await StageNotificationAsync(tenantId, request.ParentId, "Document available",
                "A document was attached to your request.", NotificationCategory.General);
            await UnitOfWork.SaveChangesAsync(ct);

            return Ok(new ParentRequestResponseDto { Id = response.Id, ResponderId = responder, Body = response.Body, RespondedAt = response.RespondedAt },
                201, "Response document added.");
        }

        public async Task<string> GetAuthorizedRequestDocumentIdAsync(string id, CancellationToken ct = default)
        {
            var request = await LoadAuthorizedAsync(id); // owner parent or SchoolAdmin only
            if (string.IsNullOrEmpty(request.FileRecordId))
                throw new NotFoundException("No document is attached to this request.");
            return request.FileRecordId!;
        }

        public async Task<string> GetAuthorizedResponseDocumentIdAsync(string id, string responseId, CancellationToken ct = default)
        {
            await LoadAuthorizedAsync(id); // authorize access to the parent request first
            var response = (await LoadResponses(id)).FirstOrDefault(r => r.Id == responseId)
                ?? throw new NotFoundException("Response not found.");
            if (string.IsNullOrEmpty(response.FileRecordId))
                throw new NotFoundException("No document is attached to this response.");
            return response.FileRecordId!;
        }

        // ---- helpers ----

        private static bool IsValidTransition(ParentRequestStatus from, ParentRequestStatus to) => from switch
        {
            ParentRequestStatus.Open => to is ParentRequestStatus.InProgress or ParentRequestStatus.Resolved or ParentRequestStatus.Rejected or ParentRequestStatus.Closed,
            ParentRequestStatus.InProgress => to is ParentRequestStatus.Resolved or ParentRequestStatus.Rejected or ParentRequestStatus.Closed,
            ParentRequestStatus.Resolved => to is ParentRequestStatus.Closed,
            ParentRequestStatus.Rejected => to is ParentRequestStatus.Closed,
            _ => false
        };

        private async Task<ParentRequest> LoadAuthorizedAsync(string id)
        {
            var caller = RequireUser();
            var request = await UnitOfWork.Repository<ParentRequest, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<ParentRequest, string>(r => r.Id == id))
                ?? throw new NotFoundException("Request not found.");
            if (IsSchoolAdmin) return request;
            if (IsParent && request.ParentId == caller) return request;
            // Hide existence from non-owners.
            throw new NotFoundException("Request not found.");
        }

        private async Task<List<ParentRequestResponse>> LoadResponses(string requestId) =>
            (await UnitOfWork.Repository<ParentRequestResponse, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<ParentRequestResponse, string>(r => r.ParentRequestId == requestId))).ToList();

        private static ParentRequestDto Map(ParentRequest r, List<ParentRequestResponse> responses) => new()
        {
            Id = r.Id, ParentId = r.ParentId, StudentId = r.StudentId, Type = r.Type, Status = r.Status,
            Title = r.Title, Body = r.Body, RequestedAt = r.RequestedAt, ResolvedAt = r.ResolvedAt,
            Responses = responses.Select(x => new ParentRequestResponseDto
            { Id = x.Id, ResponderId = x.ResponderId, Body = x.Body, RespondedAt = x.RespondedAt }).ToList()
        };
    }
}
