using System;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Domain.Common;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;

namespace DerasaX.Application.Services.Engagement
{
    /// <summary>Shared base for the Phase 5 engagement services (communities, competitions, badges, office hours).</summary>
    public abstract class EngagementServiceBase
    {
        protected readonly IUnitOfWork UnitOfWork;
        protected readonly ITenantContext Tenant;
        protected readonly IAuditWriter Audit;

        protected EngagementServiceBase(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit)
        {
            UnitOfWork = unitOfWork;
            Tenant = tenant;
            Audit = audit;
        }

        protected static DateTime AsUtc(DateTime v) => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc);

        protected string RequireTenant() =>
            Tenant.TenantId ?? throw new UnauthorizedException("Tenant context is required for this operation.");

        protected string RequireUser() =>
            Tenant.UserId ?? throw new UnauthorizedException("Authenticated user is required for this operation.");

        protected bool IsSchoolAdmin => Tenant.Role == Roles.SchoolAdmin;
        protected bool IsTeacher => Tenant.Role == Roles.Teacher;
        protected bool IsStudent => Tenant.Role == Roles.Student;

        // Phase 13 — preference-aware staging (mandatory categories are never suppressed) with honest
        // delivery-state, via the shared NotificationStaging helper. Real-time push happens after commit.
        protected Task StageNotificationAsync(string tenantId, string userId, string title, string body,
            NotificationCategory category = NotificationCategory.General) =>
            NotificationStaging.StageAsync(UnitOfWork, tenantId, userId, title, body, category,
                NotificationType.System, actorUserId: Tenant.UserId);

        protected static ApiResponse<T> Ok<T>(T data, int status, string message) =>
            new(data) { Success = true, StatusCode = status, Message = message };
    }
}
