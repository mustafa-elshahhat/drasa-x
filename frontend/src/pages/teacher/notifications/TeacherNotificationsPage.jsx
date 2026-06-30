import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Bell } from 'lucide-react'
import { Alert } from '../../../components/ui/Alert'
import { Button } from '../../../components/ui/Button'
import { PageHeader } from '../../../components/ui/PageHeader'
import { EmptyState, ErrorState } from '../../../components/ui/states'
import { toItems, toObject } from '../../../features/student/studentSchemas'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { displayValue, itemId, settledData } from '../../../features/teacher/teacherUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function NotificationsPage({ userId }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useTeacherQuery(queryKeys.teacher.notifications(userId), (signal) => teacherApi.notifications(signal))
  const mark = useMutation({ mutationFn: (id) => teacherApi.markNotificationRead(id), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.teacher.notifications(userId) }) })
  const items = query.data ? toItems(settledData(query.data.items)) : []
  const unread = query.data ? toObject(settledData(query.data.count))?.unreadCount : null
  return (
    <>
      <PageHeader title={t('teacher.notifications.title')} description={t('teacher.notifications.description')} />
      {unread !== null && unread !== undefined && <Alert variant="info" title={t('teacher.notifications.unread')}>{unread}</Alert>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {items.length === 0 ? <EmptyState icon={Bell} title={t('teacher.empty.notifications')} /> : (
        <ul className="ui-list">
          {items.map((item) => {
            const read = item.isRead ?? item.IsRead
            return (
              <li className={`ui-list__item${read ? '' : ' ui-list__item--unread'}`} key={itemId(item)}>
                <div className="ui-list__body">
                  <div className="ui-list__title">{displayValue(item)}</div>
                  <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'message', 'Message'])}</div>
                </div>
                {!read && <Button variant="ghost" onClick={() => mark.mutate(itemId(item))} loading={mark.isPending}>{t('teacher.notifications.markRead')}</Button>}
              </li>
            )
          })}
        </ul>
      )}
    </>
  )
}

export default function TeacherNotificationsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <NotificationsPage userId={userId} locale={locale} {...props} />
}
