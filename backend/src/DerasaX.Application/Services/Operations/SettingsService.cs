using System;
using System.Collections.Generic;
using System.Linq;
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

namespace DerasaX.Application.Services.Operations
{
    /// <summary>
    /// Tenant settings (SchoolAdmin, tenant-owned), platform settings + feature flags (SystemAdmin,
    /// platform-owned), and per-tenant feature evaluation. Secret values are never returned in
    /// plaintext; all changes are audited. SchoolAdmin can never touch platform settings/flags and
    /// vice versa (enforced here and by the controller policies).
    /// </summary>
    public class SettingsService : OperationsServiceBase, ISettingsService
    {
        private readonly IPlatformRepository<SystemSetting> _systemSettings;
        private readonly IPlatformRepository<FeatureFlag> _flags;

        public SettingsService(IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit,
            IPlatformRepository<SystemSetting> systemSettings, IPlatformRepository<FeatureFlag> flags) : base(uow, tenant, audit)
        {
            _systemSettings = systemSettings;
            _flags = flags;
        }

        public async Task<ApiResponse<IEnumerable<SettingDto>>> TenantSettingsAsync(CancellationToken ct = default)
        {
            RequireTenant();
            RequireSchoolAdmin();
            var items = await UnitOfWork.Repository<TenantSetting, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<TenantSetting, string>(s => true));
            return Ok<IEnumerable<SettingDto>>(items.Select(s => Map(s.Id, s.Key, s.Value, s.ValueType, s.IsSecret)).ToList());
        }

        public async Task<ApiResponse<SettingDto>> UpsertTenantSettingAsync(UpsertSettingDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            RequireSchoolAdmin();
            if (string.IsNullOrWhiteSpace(dto.Key)) throw new BadRequestException("Key is required.");

            var existing = (await UnitOfWork.Repository<TenantSetting, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<TenantSetting, string>(s => s.Key == dto.Key))).FirstOrDefault();
            if (existing is null)
            {
                existing = new TenantSetting { Id = Guid.NewGuid().ToString(), TenantId = tenantId, Key = dto.Key, Value = dto.Value, ValueType = dto.ValueType, IsSecret = dto.IsSecret };
                await UnitOfWork.Repository<TenantSetting, string>().AddAsync(existing);
            }
            else
            {
                existing.Value = dto.Value; existing.ValueType = dto.ValueType; existing.IsSecret = dto.IsSecret;
                UnitOfWork.Repository<TenantSetting, string>().Update(existing);
            }
            await Audit.StageAsync(AuditActionType.Update, nameof(TenantSetting), existing.Id, $"{{\"key\":\"{dto.Key}\"}}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(existing.Id, existing.Key, existing.Value, existing.ValueType, existing.IsSecret), 200, "Tenant setting saved.");
        }

        public async Task<ApiResponse<IEnumerable<SettingDto>>> SystemSettingsAsync(CancellationToken ct = default)
        {
            RequireSystemAdmin();
            var items = await _systemSettings.ListAsync(s => true, ct);
            return Ok<IEnumerable<SettingDto>>(items.Select(s => Map(s.Id, s.Key, s.Value, s.ValueType, s.IsSecret)).ToList());
        }

        public async Task<ApiResponse<SettingDto>> UpsertSystemSettingAsync(UpsertSettingDto dto, CancellationToken ct = default)
        {
            RequireSystemAdmin();
            if (string.IsNullOrWhiteSpace(dto.Key)) throw new BadRequestException("Key is required.");
            var existing = await _systemSettings.FirstOrDefaultAsync(s => s.Key == dto.Key, ct);
            if (existing is null)
            {
                existing = new SystemSetting { Id = Guid.NewGuid().ToString(), Key = dto.Key, Value = dto.Value, ValueType = dto.ValueType, IsSecret = dto.IsSecret };
                await _systemSettings.AddAsync(existing, ct);
            }
            else
            {
                existing.Value = dto.Value; existing.ValueType = dto.ValueType; existing.IsSecret = dto.IsSecret;
                _systemSettings.Update(existing);
            }
            // Platform settings are platform-owned (no tenant); the change is audited by the
            // entity's own IAuditable CreatedBy/UpdatedBy/UpdatedAt stamping (set automatically to
            // the acting SystemAdmin), rather than a tenant-scoped AuditLog row.
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(existing.Id, existing.Key, existing.Value, existing.ValueType, existing.IsSecret), 200, "System setting saved.");
        }

        public async Task<ApiResponse<IEnumerable<FeatureFlagDto>>> FeatureFlagsAsync(CancellationToken ct = default)
        {
            RequireSystemAdmin();
            var items = await _flags.ListAsync(f => true, ct);
            return Ok<IEnumerable<FeatureFlagDto>>(items.Select(MapFlag).ToList());
        }

        public async Task<ApiResponse<FeatureFlagDto>> UpsertFeatureFlagAsync(UpsertFeatureFlagDto dto, CancellationToken ct = default)
        {
            RequireSystemAdmin();
            if (string.IsNullOrWhiteSpace(dto.Key)) throw new BadRequestException("Key is required.");
            var existing = await _flags.FirstOrDefaultAsync(f => f.Key == dto.Key && f.TargetTenantId == dto.TargetTenantId, ct);
            if (existing is null)
            {
                existing = new FeatureFlag { Id = Guid.NewGuid().ToString(), Key = dto.Key, IsEnabled = dto.IsEnabled, TargetTenantId = dto.TargetTenantId };
                await _flags.AddAsync(existing, ct);
            }
            else
            {
                existing.IsEnabled = dto.IsEnabled;
                _flags.Update(existing);
            }
            // Platform-owned: change auditing is the flag's own IAuditable CreatedBy/UpdatedBy stamping.
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapFlag(existing), 200, "Feature flag saved.");
        }

        public async Task<ApiResponse<FeatureEvaluationDto>> EvaluateFeatureAsync(string key, CancellationToken ct = default)
        {
            var tenantId = Tenant.TenantId;
            // A flag is on for the tenant if there is an enabled global flag (no target) or an enabled
            // flag explicitly targeting this tenant.
            var flags = await _flags.ListAsync(f => f.Key == key && f.IsEnabled, ct);
            var enabled = flags.Any(f => f.TargetTenantId == null || f.TargetTenantId == tenantId);
            return Ok(new FeatureEvaluationDto { Key = key, Enabled = enabled });
        }

        private void RequireSchoolAdmin()
        {
            if (!IsSchoolAdmin) throw new ForbiddenException("Only a school administrator may manage tenant settings.");
        }

        private void RequireSystemAdmin()
        {
            if (!IsSystemAdmin) throw new ForbiddenException("Only a platform administrator may manage platform settings.");
        }

        private static SettingDto Map(string id, string key, string value, SettingValueType type, bool secret) => new()
        {
            Id = id, Key = key, Value = secret ? Redacted : value, ValueType = type, IsSecret = secret
        };

        private static FeatureFlagDto MapFlag(FeatureFlag f) => new()
        {
            Id = f.Id, Key = f.Key, IsEnabled = f.IsEnabled, TargetTenantId = f.TargetTenantId
        };
    }
}
