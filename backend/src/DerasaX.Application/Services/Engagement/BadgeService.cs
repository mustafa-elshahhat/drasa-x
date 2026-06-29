using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.EngagementDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Authorization;
using DerasaX.Application.Services.Abstractions.Engagement;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;

namespace DerasaX.Application.Services.Engagement
{
    public class BadgeService : EngagementServiceBase, IBadgeService
    {
        private readonly IPlatformRepository<Badge> _badges;
        private readonly IStudentAccessAuthorizer _access;

        public BadgeService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit,
            IPlatformRepository<Badge> badges, IStudentAccessAuthorizer access) : base(unitOfWork, tenant, audit)
        {
            _badges = badges;
            _access = access;
        }

        public async Task<ApiResponse<IEnumerable<BadgeDto>>> CatalogAsync(CancellationToken ct = default)
        {
            RequireTenant();
            var badges = await _badges.ListAsync(b => true, ct);
            var dto = badges.Select(b => new BadgeDto
            {
                Id = b.Id, Code = b.Code, Name = b.Name, Description = b.Description, Type = b.Type
            }).ToList();
            return Ok<IEnumerable<BadgeDto>>(dto, 200, "Badge catalog retrieved.");
        }

        public async Task<ApiResponse<IEnumerable<StudentBadgeDto>>> StudentBadgesAsync(string studentId, CancellationToken ct = default)
        {
            await _access.EnsureCanAccessStudentAsync(studentId, ct);
            var items = await UnitOfWork.Repository<StudentBadge, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<StudentBadge, string>(b => b.StudentId == studentId));
            return Ok<IEnumerable<StudentBadgeDto>>(items.Select(Map).ToList(), 200, "Student badges retrieved.");
        }

        public async Task<ApiResponse<StudentBadgeDto>> AwardAsync(string studentId, AwardBadgeDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (!IsTeacher && !IsSchoolAdmin) throw new ForbiddenException("Only a teacher or school administrator may award badges.");
            // Relationship rule: teacher → assigned students, SchoolAdmin → same-tenant students.
            await _access.EnsureCanAccessStudentAsync(studentId, ct);
            if (string.IsNullOrWhiteSpace(dto.BadgeId)) throw new BadRequestException("BadgeId is required.");

            var badge = await _badges.FirstOrDefaultAsync(b => b.Id == dto.BadgeId && !b.IsDeleted, ct)
                ?? throw new NotFoundException("Badge not found.");

            // Award idempotency: a badge is awarded to a student at most once.
            var existing = await UnitOfWork.Repository<StudentBadge, string>().CountAsync(
                new CriteriaSpecification<StudentBadge, string>(b => b.StudentId == studentId && b.BadgeId == badge.Id));
            if (existing > 0) throw new ConflictException("This badge has already been awarded to the student.");

            var award = new StudentBadge
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, StudentId = studentId, BadgeId = badge.Id,
                AwardedAt = DateTime.UtcNow, AwardedReason = dto.Reason
            };
            await UnitOfWork.Repository<StudentBadge, string>().AddAsync(award);
            await Audit.StageAsync(AuditActionType.Create, nameof(StudentBadge), award.Id, ct: ct);
            await StageNotificationAsync(tenantId, studentId, "Badge awarded", $"You earned the '{badge.Name}' badge!", NotificationCategory.General);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(award), 201, "Badge awarded.");
        }

        public async Task<ApiResponse<StudentStreakDto>> StreakAsync(string studentId, CancellationToken ct = default)
        {
            await _access.EnsureCanAccessStudentAsync(studentId, ct);
            var streak = (await UnitOfWork.Repository<StudentStreak, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<StudentStreak, string>(s => s.StudentId == studentId))).FirstOrDefault();
            if (streak is null)
                return Ok(new StudentStreakDto { StudentId = studentId, CurrentCount = 0, LongestCount = 0 }, 200, "Streak retrieved.");
            return Ok(MapStreak(streak), 200, "Streak retrieved.");
        }

        public async Task<ApiResponse<StudentStreakDto>> UpdateStreakAsync(string studentId, UpdateStreakDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (!IsTeacher && !IsSchoolAdmin) throw new ForbiddenException("Only a teacher or school administrator may update streaks.");
            await _access.EnsureCanAccessStudentAsync(studentId, ct);

            var date = AsUtc(dto.ActivityDate).Date;
            var streak = (await UnitOfWork.Repository<StudentStreak, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<StudentStreak, string>(s => s.StudentId == studentId))).FirstOrDefault();
            if (streak is null)
            {
                streak = new StudentStreak
                {
                    Id = Guid.NewGuid().ToString(), TenantId = tenantId, StudentId = studentId,
                    CurrentCount = 1, LongestCount = 1, LastActivityDate = date
                };
                await UnitOfWork.Repository<StudentStreak, string>().AddAsync(streak);
            }
            else
            {
                var gap = (date - streak.LastActivityDate.Date).Days;
                if (gap == 1) streak.CurrentCount += 1;
                else if (gap > 1) streak.CurrentCount = 1;
                // gap == 0 → same day, no change.
                if (streak.CurrentCount > streak.LongestCount) streak.LongestCount = streak.CurrentCount;
                streak.LastActivityDate = date;
                UnitOfWork.Repository<StudentStreak, string>().Update(streak);
            }
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapStreak(streak), 200, "Streak updated.");
        }

        private static StudentBadgeDto Map(StudentBadge b) => new()
        {
            Id = b.Id, BadgeId = b.BadgeId, StudentId = b.StudentId, AwardedAt = b.AwardedAt, AwardedReason = b.AwardedReason
        };

        private static StudentStreakDto MapStreak(StudentStreak s) => new()
        {
            Id = s.Id, StudentId = s.StudentId, CurrentCount = s.CurrentCount, LongestCount = s.LongestCount, LastActivityDate = s.LastActivityDate
        };
    }
}
