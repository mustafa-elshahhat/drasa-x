using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Domain.Common;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;

namespace DerasaX.Application.Services.Operations
{
    /// <summary>Shared helpers for the Phase 5 tenant/operations services.</summary>
    public abstract class OperationsServiceBase
    {
        protected const string Redacted = "***REDACTED***";

        protected readonly IUnitOfWork UnitOfWork;
        protected readonly ITenantContext Tenant;
        protected readonly IAuditWriter Audit;

        protected OperationsServiceBase(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit)
        {
            UnitOfWork = unitOfWork;
            Tenant = tenant;
            Audit = audit;
        }

        protected string RequireTenant() =>
            Tenant.TenantId ?? throw new UnauthorizedException("Tenant context is required for this operation.");

        protected string RequireUser() =>
            Tenant.UserId ?? throw new UnauthorizedException("Authenticated user is required for this operation.");

        protected bool IsSchoolAdmin => Tenant.Role == Roles.SchoolAdmin;
        protected bool IsSystemAdmin => Tenant.Role == Roles.SystemAdmin || Tenant.IsPlatformScope;

        protected static ApiResponse<T> Ok<T>(T data, int status = 200, string message = "OK") =>
            new(data) { Success = true, StatusCode = status, Message = message };
    }
}
