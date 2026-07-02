using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.ProvisioningDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Application.Services.Abstractions.Provisioning;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.Provisioning
{
    public class UserProvisioningService : IUserProvisioningService
    {
        private static readonly string[] ManageableRoles = { Roles.Student, Roles.Teacher, Roles.Parent };

        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;
        private readonly IAuditWriter _audit;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IPlanLimitEnforcer _limits;
        private readonly ICredentialProvisioningService _credentials;

        public UserProvisioningService(IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit,
            UserManager<ApplicationUser> users, IPlanLimitEnforcer limits, ICredentialProvisioningService credentials)
        {
            _uow = uow;
            _tenant = tenant;
            _audit = audit;
            _users = users;
            _limits = limits;
            _credentials = credentials;
        }

        private string RequireTenant() =>
            _tenant.TenantId ?? throw new UnauthorizedException("Tenant context is required for this operation.");

        public async Task<ApiResponse<ProvisionedCredentialDto>> CreateAsync(CreateTenantUserDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();

            EnglishNameValidator.Validate(dto.FullName);

            var role = NormalizeRole(dto.Role);
            if (!ManageableRoles.Contains(role))
                throw new ForbiddenException("Only Student, Teacher or Parent accounts can be provisioned here.");

            await _limits.EnsureCanAddUserAsync(tenantId, role, ct);

            string? gradeId = null;
            if (role == Roles.Student)
            {
                if (string.IsNullOrWhiteSpace(dto.GradeId))
                    throw new BadRequestException("GradeId is required for a student account.");
                var grade = await _uow.Repository<Grade, string>().GetByIdWithSpecAsync(
                    new CriteriaSpecification<Grade, string>(g => g.Id == dto.GradeId));
                if (grade is null) throw new BadRequestException("Grade not found in this tenant.");
                gradeId = dto.GradeId;
            }

            ApplicationUser user = role switch
            {
                Roles.Student => new Student { GradeId = gradeId!, Gender = dto.Gender },
                Roles.Teacher => new Teacher { Gender = dto.Gender },
                _ => new Parent { Gender = dto.Gender }
            };
            var loginCode = await _credentials.GenerateLoginCodeAsync(dto.FullName, role, ct);
            user.UserName = loginCode;
            user.FullName = dto.FullName.Trim();
            user.LoginCode = loginCode;
            user.TenantId = tenantId;
            user.IsDeleted = false;
            user.MustChangePassword = true;

            var tempPassword = _credentials.GenerateTemporaryPassword();
            var result = await _users.CreateAsync(user, tempPassword);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));
            await _users.AddToRoleAsync(user, role);

            // Audit records the account creation WITHOUT any secret material.
            await _audit.StageAsync(AuditActionType.Create, nameof(ApplicationUser), user.Id,
                $"{{\"action\":\"provision-user\",\"role\":\"{role}\"}}", ct);
            await _uow.SaveChangesAsync(ct);

            return new ApiResponse<ProvisionedCredentialDto>(true, 201, "Account provisioned successfully.",
                new ProvisionedCredentialDto
                {
                    UserId = user.Id, LoginCode = user.LoginCode, Role = role, TemporaryPassword = tempPassword
                });
        }

        public async Task<PaginationResponse<IEnumerable<TenantUserDto>>> ListAsync(TenantUserParameters p, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var role = string.IsNullOrWhiteSpace(p.Role) ? null : NormalizeRole(p.Role);

            // Only manageable role types in THIS tenant; admin accounts are never listed here.
            var query = _users.Users.Where(u => u.TenantId == tenantId &&
                (u is Student || u is Teacher || u is Parent));
            if (!p.IncludeDisabled) query = query.Where(u => !u.IsDeleted);
            if (role == Roles.Student) query = query.Where(u => u is Student);
            else if (role == Roles.Teacher) query = query.Where(u => u is Teacher);
            else if (role == Roles.Parent) query = query.Where(u => u is Parent);
            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim().ToLowerInvariant();
                query = query.Where(u => u.FullName.ToLower().Contains(s) || u.LoginCode.ToLower().Contains(s));
            }

            var total = await query.CountAsync(ct);
            var items = await query.OrderBy(u => u.FullName)
                .Skip((p.PageNumber - 1) * p.PageSize).Take(p.PageSize).ToListAsync(ct);

            var dto = items.Select(Map).ToList();
            return new PaginationResponse<IEnumerable<TenantUserDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Tenant users retrieved." };
        }

        public async Task<ApiResponse<TenantUserDto>> GetAsync(string userId, CancellationToken ct = default)
        {
            var user = await RequireManageableTenantUserAsync(userId, ct);
            return new ApiResponse<TenantUserDto>(true, 200, "Tenant user retrieved.", Map(user));
        }

        public async Task<ApiResponse<TenantUserDto>> SetEnabledAsync(string userId, bool enabled, CancellationToken ct = default)
        {
            var user = await RequireManageableTenantUserAsync(userId, ct);
            user.IsDeleted = !enabled;
            var result = await _users.UpdateAsync(user);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));

            await _audit.StageAsync(AuditActionType.Update, nameof(ApplicationUser), user.Id,
                $"{{\"action\":\"{(enabled ? "enable" : "disable")}-user\"}}", ct);
            await _uow.SaveChangesAsync(ct);
            return new ApiResponse<TenantUserDto>(true, 200, enabled ? "Account enabled." : "Account disabled.", Map(user));
        }

        public async Task<ApiResponse<ProvisionedCredentialDto>> ResetCredentialAsync(string userId, CancellationToken ct = default)
        {
            var user = await RequireManageableTenantUserAsync(userId, ct);

            var tempPassword = _credentials.GenerateTemporaryPassword();
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            user.MustChangePassword = true; // tracked before the store update below flushes both together
            var result = await _users.ResetPasswordAsync(user, token, tempPassword);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));

            await _audit.StageAsync(AuditActionType.Update, nameof(ApplicationUser), user.Id,
                "{\"action\":\"reset-credential\"}", ct);
            await _uow.SaveChangesAsync(ct);

            return new ApiResponse<ProvisionedCredentialDto>(true, 200, "Credential regenerated.",
                new ProvisionedCredentialDto
                {
                    UserId = user.Id, LoginCode = user.LoginCode, Role = RoleOf(user), TemporaryPassword = tempPassword
                });
        }

        // ---- helpers ----

        /// <summary>Loads a manageable (Student/Teacher/Parent) user in the caller's tenant, else 404 (no cross-tenant leak).</summary>
        private async Task<ApplicationUser> RequireManageableTenantUserAsync(string userId, CancellationToken ct)
        {
            var tenantId = RequireTenant();
            var user = await _users.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null || user.TenantId != tenantId || !(user is Student || user is Teacher || user is Parent))
                throw new NotFoundException("User not found.");
            return user;
        }

        private static string RoleOf(ApplicationUser u) =>
            u is Student ? Roles.Student : u is Teacher ? Roles.Teacher : u is Parent ? Roles.Parent : "Unknown";

        private static TenantUserDto Map(ApplicationUser u) => new()
        {
            Id = u.Id,
            FullName = u.FullName,
            LoginCode = u.LoginCode,
            Role = RoleOf(u),
            IsDisabled = u.IsDeleted,
            GradeId = (u as Student)?.GradeId
        };

        private static string NormalizeRole(string role) => (role ?? string.Empty).Trim() switch
        {
            var r when string.Equals(r, Roles.Student, StringComparison.OrdinalIgnoreCase) => Roles.Student,
            var r when string.Equals(r, Roles.Teacher, StringComparison.OrdinalIgnoreCase) => Roles.Teacher,
            var r when string.Equals(r, Roles.Parent, StringComparison.OrdinalIgnoreCase) => Roles.Parent,
            var r when string.Equals(r, Roles.SchoolAdmin, StringComparison.OrdinalIgnoreCase) => Roles.SchoolAdmin,
            var r when string.Equals(r, Roles.SystemAdmin, StringComparison.OrdinalIgnoreCase) => Roles.SystemAdmin,
            _ => role ?? string.Empty
        };
    }
}
