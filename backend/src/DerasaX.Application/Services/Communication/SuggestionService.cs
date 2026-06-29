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
    /// <summary>
    /// Anonymous suggestions. The submitter id is persisted for moderation/audit integrity but
    /// is NEVER returned through the school-facing list/detail APIs, so staff cannot deanonymise
    /// the author. Moderation actions are audited.
    /// </summary>
    public class SuggestionService : CommunicationServiceBase, ISuggestionService
    {
        public SuggestionService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit,
            UserManager<ApplicationUser> users) : base(unitOfWork, tenant, audit, users) { }

        public async Task<ApiResponse<SuggestionDto>> SubmitAsync(SubmitSuggestionDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var caller = RequireUser();
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Body))
                throw new BadRequestException("Title and Body are required.");

            var suggestion = new Suggestion
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                SubmittedByUserId = caller, // stored internally only — never surfaced to school staff
                Title = dto.Title,
                Body = dto.Body,
                Status = SuggestionStatus.Submitted,
                SubmittedAt = DateTime.UtcNow
            };
            await UnitOfWork.Repository<Suggestion, string>().AddAsync(suggestion);
            await Audit.StageAsync(AuditActionType.Create, nameof(Suggestion), suggestion.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(suggestion), 201, "Suggestion submitted anonymously.");
        }

        public async Task<PaginationResponse<IEnumerable<SuggestionDto>>> ListAsync(SuggestionParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            if (!IsSchoolAdmin) throw new ForbiddenException("Only school staff may view suggestions.");
            Expression<Func<Suggestion, bool>> criteria = s => !p.Status.HasValue || s.Status == p.Status.Value;
            var repo = UnitOfWork.Repository<Suggestion, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<Suggestion, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<Suggestion, string>(criteria, s => s.SubmittedAt, p.PageNumber, p.PageSize, descending: true));
            var dto = items.Select(Map).ToList();
            return new PaginationResponse<IEnumerable<SuggestionDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Suggestions retrieved." };
        }

        public async Task<ApiResponse<SuggestionDto>> ModerateAsync(string id, ModerateSuggestionDto dto, CancellationToken ct = default)
        {
            RequireTenant();
            var moderator = RequireUser();
            if (!IsSchoolAdmin) throw new ForbiddenException("Only school staff may moderate suggestions.");

            var suggestion = await UnitOfWork.Repository<Suggestion, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Suggestion, string>(s => s.Id == id))
                ?? throw new NotFoundException("Suggestion not found.");

            suggestion.Status = dto.Status;
            suggestion.ReviewNotes = dto.ReviewNotes;
            suggestion.ReviewedByUserId = moderator;
            suggestion.ReviewedAt = DateTime.UtcNow;
            UnitOfWork.Repository<Suggestion, string>().Update(suggestion);
            await Audit.StageAsync(AuditActionType.Update, nameof(Suggestion), suggestion.Id, $"{{\"status\":\"{dto.Status}\"}}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(suggestion), 200, "Suggestion moderated.");
        }

        private static SuggestionDto Map(Suggestion s) => new()
        {
            Id = s.Id, Title = s.Title, Body = s.Body, Status = s.Status, SubmittedAt = s.SubmittedAt, ReviewNotes = s.ReviewNotes
        };
    }
}
