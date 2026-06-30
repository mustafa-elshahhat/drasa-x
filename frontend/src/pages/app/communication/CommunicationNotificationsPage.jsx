import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Bell, Settings } from 'lucide-react'
import { Alert, Button, PageHeader, Spinner } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { useAuth } from '../../../features/auth/AuthContext'
import { categoryName, formatWhen } from '../../../features/communication/helpers'
import { notificationsApi } from '../../../features/notifications/notificationsApi'
import { useNotificationsRealtime } from '../../../features/notifications/NotificationsProvider'
import { STALE, queryKeys } from '../../../lib/query/keys'

function NotificationCentre() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const { connected } = useNotificationsRealtime()
  const qc = useQueryClient()
  const userId = user?.id

  const items = useQuery({
    queryKey: queryKeys.notifications.list(userId),
    queryFn: ({ signal }) => notificationsApi.list(signal),
    enabled: !!userId,
    staleTime: STALE.short,
  })
  const unread = useQuery({
    queryKey: queryKeys.notifications.unread(userId),
    queryFn: ({ signal }) => notificationsApi.unreadCount(signal),
    enabled: !!userId,
    staleTime: STALE.short,
  })

  const refresh = () => {
    qc.invalidateQueries({ queryKey: queryKeys.notifications.list(userId) })
    qc.invalidateQueries({ queryKey: queryKeys.notifications.unread(userId) })
  }
  const markRead = useMutation({ mutationFn: (id) => notificationsApi.markRead(id), onSuccess: refresh })
  const markAll = useMutation({ mutationFn: () => notificationsApi.markAllRead(), onSuccess: refresh })

  return (
    <div>
      <PageHeader
        title={t('notifications.title')}
        description={t('notifications.description')}
        actions={
          <div className="cluster">
            <Link to="/app/notifications/preferences" className="ui-btn ui-btn--ghost">
              <Settings size={16} aria-hidden="true" /> {t('notifications.preferences')}
            </Link>
            <Button variant="secondary" onClick={() => markAll.mutate()} disabled={markAll.isPending || (unread.data ?? 0) === 0}>
              {t('notifications.markAll')}
            </Button>
          </div>
        }
      />

      <Alert variant="info">
        {t('notifications.unread', { count: unread.data ?? 0 })}
        {' · '}
        <span data-testid="notifications-realtime">
          {connected ? t('notifications.realtimeOn') : t('notifications.realtimeOff')}
        </span>
      </Alert>

      {items.isLoading ? (
        <Spinner label={t('common.loading')} />
      ) : items.isError ? (
        <ErrorState error={items.error} onRetry={() => items.refetch()} />
      ) : (items.data?.length ?? 0) === 0 ? (
        <EmptyState icon={Bell} title={t('notifications.emptyTitle')} message={t('notifications.emptyBody')} />
      ) : (
        <ul className="ui-list" data-testid="notifications-list">
          {items.data.map((n) => (
            <li key={n.id} className={`ui-list__item${n.isRead ? '' : ' ui-list__item--unread'}`}>
              <div className="ui-list__body">
                <div className="ui-list__title">
                  <span className="ui-chip">{t(`notifications.category.${categoryName(n.category)}`, categoryName(n.category))}</span>{' '}
                  {n.title}
                </div>
                <div className="ui-list__meta">{n.body}</div>
                <div className="ui-list__meta ui-muted">{formatWhen(n.createdAt)}</div>
                {n.actionUrl && (
                  <Link to={n.actionUrl} className="ui-link">
                    {t('notifications.open')}
                  </Link>
                )}
              </div>
              {!n.isRead && (
                <Button variant="ghost" onClick={() => markRead.mutate(n.id)} disabled={markRead.isPending}>
                  {t('notifications.markRead')}
                </Button>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

// --------------------------------------------------------------------------
// Notification preferences
// --------------------------------------------------------------------------

export default NotificationCentre
