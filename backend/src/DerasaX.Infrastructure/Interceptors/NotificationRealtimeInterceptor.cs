using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.NotificationDto;
using DerasaX.Application.Services.Abstractions.Notification;
using DerasaX.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DerasaX.Infrastructure.Interceptors
{
    /// <summary>
    /// Phase 13 — the single, universal real-time push point. Any code that inserts a
    /// <see cref="Notification"/> (the service-base staging helpers, announcements, support, the
    /// notification service…) gets a best-effort SignalR push to the recipient's user group AFTER the
    /// transaction commits — so the authenticated frontend sees new notifications without a refresh, and
    /// there is exactly one real-time event per persisted notification. The durable in-app row is the
    /// source of truth; a failed push never affects it (the inbox re-syncs on the next fetch/reconnect),
    /// which is why real-time delivery is honestly best-effort rather than guaranteed.
    ///
    /// Registered per-scope on the DbContext, so the captured-pending list is per-request and thread-safe.
    /// </summary>
    public sealed class NotificationRealtimeInterceptor : SaveChangesInterceptor
    {
        private readonly IRealtimeSender _realtime;
        private readonly List<Notification> _pending = new();

        public NotificationRealtimeInterceptor(IRealtimeSender realtime) => _realtime = realtime;

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Capture(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Capture(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            FlushAsync().GetAwaiter().GetResult();
            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            await FlushAsync();
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        private void Capture(DbContext? context)
        {
            if (context is null) return;
            foreach (var entry in context.ChangeTracker.Entries<Notification>())
            {
                if (entry.State == EntityState.Added && !string.IsNullOrEmpty(entry.Entity.UserId))
                    _pending.Add(entry.Entity);
            }
        }

        private async Task FlushAsync()
        {
            if (_pending.Count == 0) return;
            var batch = _pending.ToList();
            _pending.Clear();

            foreach (var n in batch)
            {
                try
                {
                    await _realtime.SendToUserAsync(n.UserId!, new NotificationDto
                    {
                        Title = n.Title,
                        Body = n.Body,
                        ActionUrl = n.ActionUrl,
                        Category = n.NotificationCategory,
                        Type = n.NotificationType,
                        MetadataJson = n.MetadataJson
                    });
                }
                catch
                {
                    // Real-time is best-effort; the durable in-app notification already succeeded and the
                    // inbox re-syncs on the next fetch/reconnect. Never throw from the save pipeline.
                }
            }
        }
    }
}
