// =============================================================================
// Phase 13 — header notification bell. Shows the REAL unread count from
// /api/v1/notifications/unread-count (never a fabricated badge). When the SignalR
// hub is connected the count refreshes purely from real-time invalidation (no
// polling). When real-time is unavailable it falls back to a slow 60s poll — an
// honest, documented fallback, not polling disguised as real-time.
// =============================================================================
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Bell } from 'lucide-react'
import { useAuth } from '../auth/AuthContext'
import { queryKeys, STALE } from '../../lib/query/keys'
import { notificationsApi } from './notificationsApi'
import { useNotificationsRealtime } from './NotificationsProvider'

export function NotificationBell() {
  const { t } = useTranslation()
  const { user, isAuthenticated } = useAuth()
  const { connected } = useNotificationsRealtime()

  const { data: count = 0 } = useQuery({
    queryKey: queryKeys.notifications.unread(user?.id),
    queryFn: ({ signal }) => notificationsApi.unreadCount(signal),
    enabled: isAuthenticated && !!user?.id,
    staleTime: STALE.short,
    // Poll ONLY when the real-time hub is not connected (documented fallback).
    refetchInterval: connected ? false : 60_000,
  })

  if (!isAuthenticated) return null

  const label = count > 0 ? t('notifications.bellUnread', { count }) : t('notifications.bell')

  return (
    <Link to="/app/notifications" className="app-header__bell" aria-label={label} data-testid="notification-bell">
      <Bell size={18} aria-hidden="true" />
      {count > 0 && (
        <span className="app-header__bell-badge" data-testid="notification-bell-count">
          {count > 99 ? '99+' : count}
        </span>
      )}
      <span
        className="app-header__bell-status"
        data-realtime={connected ? 'on' : 'off'}
        title={connected ? t('notifications.realtimeOn') : t('notifications.realtimeOff')}
        aria-hidden="true"
      />
    </Link>
  )
}
