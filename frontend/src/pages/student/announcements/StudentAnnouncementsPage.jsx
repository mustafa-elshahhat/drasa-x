import { useTranslation } from 'react-i18next'
import { Megaphone } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { PageHeader } from '../../../components/ui/PageHeader'
import { QueryBoundary } from '../../../components/ui/QueryBoundary'
import { ErrorState } from '../../../components/ui/states'
import { useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { displayValue, formatDate, getField, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'

function AnnouncementsPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useStudentQuery(queryKeys.student.announcements(userId), (signal) => studentApi.announcements(signal), { staleTime: STALE.medium })
  return (
    <>
      <PageHeader title={t('student.announcements.title')} description={t('student.announcements.description')} />
      <QueryBoundary query={query} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.announcements')} emptyIcon={Megaphone}>
        {(items) => (
          <div className="student-list">
            {items.map((item) => (
              <div className="ui-list__item" key={itemId(item)} style={{ borderInlineStart: '3px solid var(--accent)' }}>
                <div className="ui-list__body">
                  <div className="ui-list__title">{displayValue(item)}</div>
                  <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'message', 'Message', 'content', 'Content'])}</div>
                  <div className="ui-list__meta ui-muted">{formatDate(getField(item, 'createdAt') || getField(item, 'publishedAt'), locale)}</div>
                </div>
              </div>
            ))}
          </div>
        )}
      </QueryBoundary>
    </>
  )
}

export default function StudentAnnouncementsPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <AnnouncementsPage userId={userId} locale={locale} {...props} />
}
