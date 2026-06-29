using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.NotificationDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Notification;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;

namespace DerasaX.Application.Services.Notification
{
    /// <inheritdoc />
    public class NotificationPreferenceService : INotificationPreferenceService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;
        private readonly IAuditWriter _audit;

        public NotificationPreferenceService(IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit)
        {
            _uow = uow;
            _tenant = tenant;
            _audit = audit;
        }

        private string RequireTenant() =>
            _tenant.TenantId ?? throw new UnauthorizedException("Tenant context is required for this operation.");

        private string RequireUser() =>
            _tenant.UserId ?? throw new UnauthorizedException("Authenticated user is required for this operation.");

        public async Task<ApiResponse<IEnumerable<NotificationPreferenceItemDto>>> GetMineAsync(CancellationToken ct = default)
        {
            RequireTenant();
            var userId = RequireUser();

            var stored = (await _uow.Repository<NotificationPreference, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<NotificationPreference, string>(p => p.UserId == userId)))
                .ToDictionary(p => p.Category);

            var items = NotificationPolicy.All.Select(cat => Map(cat, stored.GetValueOrDefault(cat))).ToList();
            return new ApiResponse<IEnumerable<NotificationPreferenceItemDto>>(items)
            { Success = true, StatusCode = 200, Message = "Notification preferences retrieved." };
        }

        public async Task<ApiResponse<NotificationPreferenceItemDto>> UpdateMineAsync(UpdateNotificationPreferenceDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var userId = RequireUser();

            // Mandatory categories (e.g. security warnings) can never be disabled in-app.
            if (NotificationPolicy.IsMandatory(dto.Category) && !dto.InAppEnabled)
                throw new BadRequestException($"The '{dto.Category}' category is mandatory and cannot be disabled.");

            var existing = (await _uow.Repository<NotificationPreference, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<NotificationPreference, string>(p => p.UserId == userId && p.Category == dto.Category)))
                .FirstOrDefault();

            // Mandatory categories are always stored/served as enabled regardless of the request.
            var effectiveInApp = NotificationPolicy.IsMandatory(dto.Category) || dto.InAppEnabled;

            if (existing is null)
            {
                existing = new NotificationPreference
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    UserId = userId,
                    Category = dto.Category,
                    InAppEnabled = effectiveInApp,
                    EmailEnabled = dto.EmailEnabled,
                    CreatedAt = DateTime.UtcNow
                };
                await _uow.Repository<NotificationPreference, string>().AddAsync(existing);
            }
            else
            {
                existing.InAppEnabled = effectiveInApp;
                existing.EmailEnabled = dto.EmailEnabled;
                existing.UpdatedAt = DateTime.UtcNow;
                _uow.Repository<NotificationPreference, string>().Update(existing);
            }

            await _audit.StageAsync(AuditActionType.Update, nameof(NotificationPreference), existing.Id,
                $"{{\"category\":\"{dto.Category}\",\"inApp\":{effectiveInApp.ToString().ToLowerInvariant()},\"email\":{dto.EmailEnabled.ToString().ToLowerInvariant()}}}", ct);
            await _uow.SaveChangesAsync(ct);

            return new ApiResponse<NotificationPreferenceItemDto>(Map(dto.Category, existing))
            { Success = true, StatusCode = 200, Message = "Notification preference updated." };
        }

        private static NotificationPreferenceItemDto Map(NotificationCategory category, NotificationPreference? pref)
        {
            var mandatory = NotificationPolicy.IsMandatory(category);
            return new NotificationPreferenceItemDto
            {
                Category = category,
                CategoryName = category.ToString(),
                InAppEnabled = mandatory || (pref?.InAppEnabled ?? true),
                EmailEnabled = pref?.EmailEnabled ?? false,
                Mandatory = mandatory,
                EmailConfigured = false // no e-mail sender/outbox configured in this environment (honest)
            };
        }
    }
}
