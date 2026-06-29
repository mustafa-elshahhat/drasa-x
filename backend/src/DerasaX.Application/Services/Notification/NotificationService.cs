using DerasaX.Application.Common;
using DerasaX.Application.Dto.NotificationDto;
using DerasaX.Application.Services.Abstractions.Notification;
using DerasaX.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DerasaX.Domain.Entities.Models;

namespace DerasaX.Application.Services.Notification
{
    /// <summary>
    /// Phase 13 — direct notification send + the current-user inbox. Persistence routes through the
    /// shared <see cref="NotificationStaging"/> helper so every send honours per-user preferences and sets
    /// the honest delivery-state fields. Real-time push is NOT done here: the
    /// <c>NotificationRealtimeInterceptor</c> pushes every newly-inserted notification to its user group
    /// after the transaction commits, so there is exactly one real-time event per notification.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task SendToUserAsync(string userId, NotificationDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || string.IsNullOrEmpty(user.TenantId)) return;

            await NotificationStaging.StageAsync(_unitOfWork, user.TenantId, user.Id, dto.Title, dto.Body,
                dto.Category, dto.Type, dto.ActionUrl, actorUserId: null, metadataJson: dto.MetadataJson);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task SendToTenantRoleAsync(string tenantId, string role, NotificationDto dto)
        {
            var tenantUsers = await _userManager.Users.Where(u => u.TenantId == tenantId).ToListAsync();
            var staged = false;
            foreach (var user in tenantUsers)
            {
                if (!await _userManager.IsInRoleAsync(user, role)) continue;
                var n = await NotificationStaging.StageAsync(_unitOfWork, tenantId, user.Id, dto.Title, dto.Body,
                    dto.Category, dto.Type, dto.ActionUrl, actorUserId: null, metadataJson: dto.MetadataJson);
                staged |= n is not null;
            }
            if (staged) await _unitOfWork.SaveChangesAsync();
        }

        public async Task SendAnnouncementAsync(string tenantId, NotificationDto dto)
        {
            var allUsers = await _userManager.Users.Where(u => u.TenantId == tenantId).ToListAsync();
            var staged = false;
            foreach (var user in allUsers)
            {
                var n = await NotificationStaging.StageAsync(_unitOfWork, tenantId, user.Id, dto.Title, dto.Body,
                    dto.Category, dto.Type, dto.ActionUrl, actorUserId: null, metadataJson: dto.MetadataJson);
                staged |= n is not null;
            }
            if (staged) await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<NotificationResponseDto>> GetNotificationsAsync(string userId, int page = 1, int pageSize = 20)
        {
            var spec = new Domain.Specification.NotificationByUserSpecification(userId, page, pageSize);
            var notifications = await _unitOfWork.Repository<Domain.Entities.Models.Notification, string>()
                .GetAllWithSpecAsync(spec);

            return notifications.Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                Title = n.Title,
                Body = n.Body,
                ActionUrl = n.ActionUrl,
                Category = n.NotificationCategory,
                Type = n.NotificationType,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt,
                EmailStatus = n.EmailStatus,
                MetadataJson = n.MetadataJson
            });
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            var spec = new Domain.Specification.UnreadNotificationCountSpecification(userId);
            return await _unitOfWork.Repository<Domain.Entities.Models.Notification, string>()
                .CountAsync(spec);
        }

        public async Task<bool> MarkAsReadAsync(string notificationId, string userId)
        {
            var notification = await _unitOfWork.Repository<Domain.Entities.Models.Notification, string>()
                .GetByIdAsync(notificationId);

            if (notification is null || notification.UserId != userId)
                return false;

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                _unitOfWork.Repository<Domain.Entities.Models.Notification, string>().Update(notification);
                await _unitOfWork.SaveChangesAsync();
            }
            return true;
        }

        public async Task MarkAllReadAsync(string userId)
        {
            var spec = new Domain.Specification.AllNotificationsByUserSpecification(userId);
            var notifications = (await _unitOfWork.Repository<Domain.Entities.Models.Notification, string>()
                .GetAllWithSpecAsync(spec)).Where(n => !n.IsRead).ToList();

            var now = DateTime.UtcNow;
            foreach (var n in notifications)
            {
                n.IsRead = true;
                n.ReadAt = now;
            }

            // Bulk update: all changes tracked in the same context, single SaveChanges.
            if (notifications.Count > 0)
                await _unitOfWork.SaveChangesAsync();
        }
    }
}
