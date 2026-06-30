import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Alert, Chip, Card, PageHeader } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { Heatmap } from '../../../shared/charts'
import { Loading, Stat } from '../../../features/parent/components'
import { ATTENDANCE_TONE, useParentQuery } from '../../../features/parent/helpers'
import { parentApi } from '../../../features/parent/parentApi'
import { formatDate } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function ChildAttendancePage({ userId, locale }) {
  const { t } = useTranslation()
  const { childId } = useParams()
  const query = useParentQuery(queryKeys.parent.childAttendance(userId, childId), (signal) => parentApi.childAttendance(childId, signal))
  const att = query.data
  const legend = [
    { level: 'present', label: t('parent.attendance.present') },
    { level: 'late', label: t('parent.attendance.late') },
    { level: 'absent', label: t('parent.attendance.absent') },
    { level: 'excused', label: t('parent.attendance.excused') },
  ]
  const cells = (att?.records || []).map((r, i) => {
    const level = (String(r.status || '').toLowerCase()) in ATTENDANCE_TONE ? String(r.status).toLowerCase() : 'none'
    return { key: r.id ?? i, level, title: `${formatDate(r.attendanceDate, locale)} — ${r.status}` }
  })
  return (
    <>
      <PageHeader title={t('parent.attendance.title')} description={t('parent.attendance.description')} />
      <Alert title={t('parent.attendance.noCvTitle')}>{t('parent.attendance.noCvBody')}</Alert>
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {att && (
        <>
          <Card title={t('parent.attendance.summary')}>
            <div className="domain-child__stats">
              <Stat label={t('parent.attendance.total')} value={att.summary.total} />
              <Stat label={t('parent.attendance.present')} value={att.summary.present} />
              <Stat label={t('parent.attendance.absent')} value={att.summary.absent} />
              <Stat label={t('parent.attendance.late')} value={att.summary.late} />
              <Stat label={t('parent.attendance.excused')} value={att.summary.excused} />
              <Stat label={t('parent.attendance.percentage')} value={att.summary.attendancePercentage} />
            </div>
            {cells.length > 0 && <div className="mt-[18px]"><Heatmap cells={cells} legend={legend} /></div>}
          </Card>
          <Card title={t('parent.attendance.records')}>
            {(att.records || []).length === 0 ? <EmptyState title={t('parent.attendance.empty')} /> : (
              <ul className="ui-list">
                {att.records.map((row, i) => {
                  const tone = ATTENDANCE_TONE[String(row.status || '').toLowerCase()] || 'muted'
                  return (
                    <li className="ui-list__item" key={row.id || i}>
                      <div className="ui-list__body">
                        <div className="ui-list__title">{formatDate(row.attendanceDate, locale)}</div>
                        {row.source && <div className="ui-list__meta ui-muted">{row.source}</div>}
                      </div>
                      <Chip tone={tone}>{row.status}</Chip>
                    </li>
                  )
                })}
              </ul>
            )}
          </Card>
        </>
      )}
    </>
  )
}

// ---------------------------------------------------------------------------
// Document requests (create + track)
// ---------------------------------------------------------------------------
// ParentRequestType enum values (the backend binds the enum numerically).

export default function ParentChildAttendancePage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <ChildAttendancePage userId={userId} locale={locale} {...props} />
}
