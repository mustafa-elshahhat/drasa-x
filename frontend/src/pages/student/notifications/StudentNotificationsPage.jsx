import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Bell } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { PageHeader } from '../../../components/ui/PageHeader'
import { EmptyState, ErrorState } from '../../../components/ui/states'
import { useStudentQuery } from '../../../features/student/helpers'
import { studentApi } from '../../../features/student/studentApi'
import { toItems, toObject } from '../../../features/student/studentSchemas'
import { displayValue, formatDate, getField, itemId, settledData } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'

function NotificationsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useStudentQuery(queryKeys.student.notifications(userId), (signal) => studentApi.notifications(signal))
  const mark = useMutation({ mutationFn: (id) => studentApi.markNotificationRead(id), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.notifications(userId) }) })
  const markAll = useMutation({ mutationFn: () => studentApi.markAllNotificationsRead(), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.notifications(userId) }) })
  const items = query.data ? toItems(settledData(query.data.items)) : []
  const unread = query.data ? toObject(settledData(query.data.count))?.unreadCount : null
  return (
    <>
      <PageHeader title={t('student.notifications.title')} description={t('student.notifications.description')} actions={<Button variant="secondary" onClick={() => markAll.mutate()} loading={markAll.isPending}>{t('student.notifications.markAll')}</Button>} />
      {unread !== null && <Alert variant="info" title={t('student.notifications.unread')}>{unread}</Alert>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {mark.isError && <ErrorState error={mark.error} />}
      {items.length === 0 ? <EmptyState icon={Bell} title={t('student.empty.notifications')} /> : (
        <ul className="ui-list">
          {items.map((item) => {
            const read = getField(item, 'isRead')
            return (
              <li className={`ui-list__item${read ? '' : ' ui-list__item--unread'}`} key={itemId(item)}>
                <div className="ui-list__body">
                  <div className="ui-list__title">{displayValue(item)}</div>
                  <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'message', 'Message'])}</div>
                  <div className="ui-list__meta ui-muted">{formatDate(getField(item, 'createdAt'), locale)}</div>
                </div>
                {!read && <Button variant="ghost" onClick={() => mark.mutate(itemId(item))} loading={mark.isPending}>{t('student.notifications.markRead')}</Button>}
              </li>
            )
          })}
        </ul>
      )}
    </>
  )
}

export default function StudentNotificationsPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <NotificationsPage userId={userId} locale={locale} {...props} />
}
