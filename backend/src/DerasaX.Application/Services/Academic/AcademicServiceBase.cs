using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;

namespace DerasaX.Application.Services.Academic
{
    /// <summary>
    /// Shared base for tenant-scoped academic services. Centralises trusted-tenant
    /// resolution so no service reads tenant from request input. All queries run
    /// through the unit of work, whose DbContext applies the global tenant filter.
    /// </summary>
    public abstract class AcademicServiceBase
    {
        protected readonly IUnitOfWork UnitOfWork;
        protected readonly ITenantContext Tenant;
        protected readonly IAuditWriter Audit;

        protected AcademicServiceBase(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit)
        {
            UnitOfWork = unitOfWork;
            Tenant = tenant;
            Audit = audit;
        }

        /// <summary>
        /// Normalises an inbound date to UTC. JSON dates without an explicit offset
        /// deserialize as <see cref="System.DateTimeKind.Unspecified"/>, which Npgsql
        /// rejects for <c>timestamptz</c> columns; the provided instant is treated as UTC.
        /// </summary>
        protected static System.DateTime AsUtc(System.DateTime value) =>
            value.Kind == System.DateTimeKind.Utc
                ? value
                : System.DateTime.SpecifyKind(value, System.DateTimeKind.Utc);

        /// <summary>Returns the trusted tenant id from the access-token claim, or throws.</summary>
        protected string RequireTenant()
        {
            var tenantId = Tenant.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new UnauthorizedException("Tenant context is required for this operation.");
            return tenantId;
        }
    }
}
