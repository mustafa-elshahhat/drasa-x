import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Bell } from 'lucide-react'
import { Alert, Button, PageHeader } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { useParentQuery } from '../../../features/parent/helpers'
import { parentApi } from '../../../features/parent/parentApi'
import { toItems, toObject } from '../../../features/student/studentSchemas'
import { displayValue, formatDate, itemId, settledData } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function NotificationsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useParentQuery(queryKeys.parent.notifications(userId), (signal) => parentApi.notifications(signal))
  const mark = useMutation({ mutationFn: (id) => parentApi.markNotificationRead(id), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.parent.notifications(userId) }) })
  const items = query.data ? toItems(settledData(query.data.items)) : []
  const unread = query.data ? toObject(settledData(query.data.count))?.unreadCount : null
  return (
    <>
      <PageHeader title={t('parent.notifications.title')} description={t('parent.notifications.description')} />
      {unread !== null && unread !== undefined && <Alert variant="info" title={t('parent.notifications.unread')}>{unread}</Alert>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {items.length === 0 ? <EmptyState icon={Bell} title={t('parent.empty.notifications')} /> : (
        <ul className="ui-list">
          {items.map((item) => {
            const read = item.isRead ?? item.IsRead
            return (
              <li className={`ui-list__item${read ? '' : ' ui-list__item--unread'}`} key={itemId(item)}>
                <div className="ui-list__body">
                  <div className="ui-list__title">{displayValue(item) || itemId(item)}</div>
                  <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'message', 'Message'])}</div>
                  <div className="ui-list__meta ui-muted">{formatDate(item.createdAt ?? item.CreatedAt, locale)}</div>
                </div>
                {!read && <Button variant="ghost" onClick={() => mark.mutate(itemId(item))} loading={mark.isPending}>{t('parent.notifications.markRead')}</Button>}
              </li>
            )
          })}
        </ul>
      )}
    </>
  )
}

export default function ParentNotificationsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <NotificationsPage userId={userId} locale={locale} {...props} />
}
