using System;
using System.Threading;
using System.Threading.Tasks;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.Operations
{
    /// <summary>
    /// Bounded reports backed by real authoritative data, scoped to the caller's tenant. Date
    /// ranges are validated; no metric is fabricated when its source data does not exist.
    /// </summary>
    public class ReportService : OperationsServiceBase, IReportService
    {
        private readonly UserManager<ApplicationUser> _users;

        public ReportService(IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit, UserManager<ApplicationUser> users)
            : base(uow, tenant, audit)
        {
            _users = users;
        }

        public async Task<ApiResponse<TenantUsersReportDto>> TenantUsersAsync(CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            return Ok(new TenantUsersReportDto
            {
                Students = await _users.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted && u is Student, ct),
                Teachers = await _users.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted && u is Teacher, ct),
                Parents = await _users.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted && u is Parent, ct),
                Admins = await _users.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted && u is SchoolAdmin, ct)
            });
        }

        public async Task<ApiResponse<ActivityReportDto>> AssessmentSummaryAsync(ReportParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            var (from, to) = Range(p);
            var count = await UnitOfWork.Repository<QuizSubmission, string>().CountAsync(
                new CriteriaSpecification<QuizSubmission, string>(s =>
                    s.submissionStatus != SubmissionStatus.InProgress &&
                    (from == null || s.SubmittedAt >= from) && (to == null || s.SubmittedAt <= to)));
            return Ok(Activity("assessment-submissions", count, from, to));
        }

        public async Task<ApiResponse<ActivityReportDto>> AuditActivityAsync(ReportParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            var (from, to) = Range(p);
            var count = await UnitOfWork.Repository<AuditLog, string>().CountAsync(
                new CriteriaSpecification<AuditLog, string>(a =>
                    (from == null || a.OccurredAt >= from) && (to == null || a.OccurredAt <= to)));
            return Ok(Activity("audit-events", count, from, to));
        }

        public async Task<ApiResponse<ActivityReportDto>> AiUsageActivityAsync(ReportParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            var (from, to) = Range(p);
            var count = await UnitOfWork.Repository<AiUsageRecord, string>().CountAsync(
                new CriteriaSpecification<AiUsageRecord, string>(a =>
                    (from == null || a.UsedAt >= from) && (to == null || a.UsedAt <= to)));
            return Ok(Activity("ai-usage", count, from, to));
        }

        private static (DateTime? from, DateTime? to) Range(ReportParameters p)
        {
            DateTime? from = p.From.HasValue ? AsUtc(p.From.Value) : null;
            DateTime? to = p.To.HasValue ? AsUtc(p.To.Value) : null;
            if (from.HasValue && to.HasValue && to < from) throw new BadRequestException("'To' must be on or after 'From'.");
            return (from, to);
        }

        private static DateTime AsUtc(DateTime v) => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc);

        private static ActivityReportDto Activity(string kind, int count, DateTime? from, DateTime? to) =>
            new() { Kind = kind, Count = count, From = from, To = to };
    }
}
