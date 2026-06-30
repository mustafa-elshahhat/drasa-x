using System;
using System.Collections.Generic;
using System.Linq;
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
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.Communication
{
    public class AnnouncementService : CommunicationServiceBase, IAnnouncementService
    {
        public AnnouncementService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit,
            UserManager<ApplicationUser> users) : base(unitOfWork, tenant, audit, users) { }

        public async Task<ApiResponse<AnnouncementDto>> CreateAsync(CreateAnnouncementDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (!IsSchoolAdmin) throw new ForbiddenException("Only a school administrator may create announcements.");
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Body))
                throw new BadRequestException("Title and Body are required.");
            if (dto.TargetAudience == TargetAudience.None)
                throw new BadRequestException("A target audience must be specified.");

            var announcement = new Announcement
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                Title = dto.Title,
                Body = dto.Body,
                TargetAudience = dto.TargetAudience,
                ExpiresAt = AsUtc(dto.ExpiresAt),
                // Draft → Publish model: a created announcement is an inactive DRAFT. It becomes visible
                // and fans out notifications only when explicitly published. This keeps "active in list"
                // and "recipients notified" from diverging.
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };
            await UnitOfWork.Repository<Announcement, string>().AddAsync(announcement);
            await Audit.StageAsync(AuditActionType.Create, nameof(Announcement), announcement.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(announcement), 201, "Announcement draft created.");
        }

        public async Task<PaginationResponse<IEnumerable<AnnouncementDto>>> ListAsync(AnnouncementParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            var repo = UnitOfWork.Repository<Announcement, string>();

            // SchoolAdmin sees all tenant announcements (DB-paged).
            if (IsSchoolAdmin)
            {
                System.Linq.Expressions.Expression<Func<Announcement, bool>> all = a => true;
                var totalAll = await repo.CountAsync(new CriteriaSpecification<Announcement, string>(all));
                var pageAll = await repo.GetAllWithSpecAsync(
                    new PagedSpecification<Announcement, string>(all, a => a.CreatedAt, p.PageNumber, p.PageSize, descending: true));
                return Page(pageAll.Select(Map).ToList(), totalAll, p);
            }

            // Others: active, non-expired announcements targeting their audience. TargetAudience is
            // a [Flags] enum persisted as a string, so the bitwise audience match is done in memory
            // over the (bounded, low-cardinality) active set rather than in SQL.
            var now = DateTime.UtcNow;
            var audience = AudienceFor(Tenant.Role);
            var active = await repo.GetAllWithSpecAsync(new CriteriaSpecification<Announcement, string>(
                a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt >= now)));
            var matched = active.Where(a => (a.TargetAudience & audience) != 0)
                .OrderByDescending(a => a.CreatedAt).ToList();
            var paged = matched.Skip((p.PageNumber - 1) * p.PageSize).Take(p.PageSize).Select(Map).ToList();
            return Page(paged, matched.Count, p);
        }

        private static PaginationResponse<IEnumerable<AnnouncementDto>> Page(IEnumerable<AnnouncementDto> items, int total, AnnouncementParameters p) =>
            new(items, total, p.PageNumber, p.PageSize) { Success = true, StatusCode = 200, Message = "Announcements retrieved." };

        public async Task<ApiResponse<AnnouncementDto>> PublishAsync(string id, bool publish, CancellationToken ct = default)
        {
            RequireTenant();
            if (!IsSchoolAdmin) throw new ForbiddenException("Only a school administrator may publish announcements.");
            var announcement = await UnitOfWork.Repository<Announcement, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Announcement, string>(a => a.Id == id))
                ?? throw new NotFoundException("Announcement not found.");
            var wasActive = announcement.IsActive;
            announcement.IsActive = publish;
            UnitOfWork.Repository<Announcement, string>().Update(announcement);
            await Audit.StageAsync(AuditActionType.Update, nameof(Announcement), announcement.Id, $"{{\"publish\":{publish.ToString().ToLowerInvariant()}}}", ct);

            // Phase 13 — publishing (publish:true) is the moment we broadcast: fan out an in-app
            // notification to every targeted same-tenant user honoring TargetAudience. Recipients-only
            // (a teacher never receives a Students-targeted announcement). The create path stays
            // notification-free so a draft/edit never spams the tenant. Fan-out only on the
            // inactive→active transition, so re-publishing an already-active announcement never duplicates.
            if (publish && !wasActive)
                await EmitAnnouncementNotificationsAsync(announcement, ct);

            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(announcement), 200, publish ? "Announcement published." : "Announcement unpublished.");
        }

        // Fans out an Announcement-category notification to the targeted same-tenant users. One bounded
        // user query (by tenant) + one batched preference query + N inserts (no N+1). Targeting uses the
        // TPT CLR type (Student/Parent/Teacher), so it never leaks to a non-targeted role. Announcement is
        // an optional category, so a recipient who disabled it in-app is honoured (suppressed). Real-time
        // push to each recipient happens after commit via the notification interceptor.
        private async Task EmitAnnouncementNotificationsAsync(Announcement a, CancellationToken ct)
        {
            var tenantId = a.TenantId!;
            var audience = a.TargetAudience;

            var tenantUsers = await Users.Users
                .Where(u => u.TenantId == tenantId && !u.IsDeleted)
                .ToListAsync(ct);

            bool Targeted(ApplicationUser u) =>
                (audience.HasFlag(TargetAudience.Students) && u is Student) ||
                (audience.HasFlag(TargetAudience.Parents) && u is Parent) ||
                (audience.HasFlag(TargetAudience.Teachers) && u is Teacher);

            var recipients = tenantUsers.Where(Targeted).Select(u => u.Id).ToList();
            if (recipients.Count == 0)
            {
                await Audit.StageAsync(AuditActionType.Update, nameof(Announcement), a.Id,
                    $"{{\"action\":\"announcement-targeting\",\"audience\":\"{audience}\",\"recipients\":0}}", ct);
                return;
            }

            // Batched suppression lookup: recipients who turned the (optional) Announcement category off.
            var suppressed = (await UnitOfWork.Repository<NotificationPreference, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<NotificationPreference, string>(p =>
                        recipients.Contains(p.UserId) && p.Category == NotificationCategory.Announcement && !p.InAppEnabled)))
                .Select(p => p.UserId).ToHashSet();

            var body = a.Body.Length > 240 ? a.Body[..240] : a.Body;
            var delivered = 0;
            foreach (var uid in recipients)
            {
                if (suppressed.Contains(uid)) continue;
                await UnitOfWork.Repository<Domain.Entities.Models.Notification, string>().AddAsync(new Domain.Entities.Models.Notification
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    UserId = uid,
                    Title = a.Title,
                    Body = body,
                    ActionUrl = "/app/notifications",
                    NotificationCategory = NotificationCategory.Announcement,
                    NotificationType = NotificationType.Announcement,
                    ActorUserId = Tenant.UserId,
                    MetadataJson = $"{{\"announcementId\":\"{a.Id}\"}}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    DeliveryStatus = NotificationChannelStatus.Delivered,
                    EmailStatus = NotificationChannelStatus.NotConfigured
                });
                delivered++;
            }

            await Audit.StageAsync(AuditActionType.Update, nameof(Announcement), a.Id,
                $"{{\"action\":\"announcement-targeting\",\"audience\":\"{audience}\",\"recipients\":{delivered}}}", ct);
        }

        private static TargetAudience AudienceFor(string? role) => role switch
        {
            Roles.Student => TargetAudience.Students,
            Roles.Parent => TargetAudience.Parents,
            Roles.Teacher => TargetAudience.Teachers,
            Roles.SchoolAdmin => TargetAudience.All,
            _ => TargetAudience.None
        };

        private static DateTime? AsUtc(DateTime? v) => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)) : null;

        private static AnnouncementDto Map(Announcement a) => new()
        {
            Id = a.Id, Title = a.Title, Body = a.Body, TargetAudience = a.TargetAudience,
            IsActive = a.IsActive, CreatedAt = a.CreatedAt, ExpiresAt = a.ExpiresAt
        };
    }
}
