import { useTranslation } from 'react-i18next'
import { useStudentContext } from '../../../features/student/helpers'
import { Alert, Chip, Card, PageHeader } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { Heatmap, Ring } from '../../../shared/charts'
import { ATTENDANCE_TONE, useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { formatDate } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'

function AttendancePage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useStudentQuery(queryKeys.student.attendance(userId), (signal) => studentApi.attendance(signal))
  return (
    <>
      <PageHeader title={t('student.attendance.title')} description={t('student.attendance.description')} />
      <Alert title={t('student.attendance.noCvTitle')}>{t('student.attendance.noCvBody')}</Alert>
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && <AttendanceContent data={query.data} locale={locale} />}
    </>
  )
}

function AttendanceContent({ data, locale }) {
  const { t } = useTranslation()
  const records = data.records || []
  const summary = data.summary || {}
  const pct = summary.attendancePercentage ?? 0
  const legend = [
    { level: 'present', label: t('student.attendance.present') },
    { level: 'late', label: t('student.attendance.late') },
    { level: 'absent', label: t('student.attendance.absent') },
    { level: 'excused', label: t('student.attendance.excused') },
  ]
  const cells = records.map((r, i) => {
    const level = (String(r.status || '').toLowerCase()) in ATTENDANCE_TONE ? String(r.status).toLowerCase() : 'none'
    return { key: r.id ?? i, level, title: `${formatDate(r.attendanceDate, locale)} — ${r.status}` }
  })
  return (
    <>
      <Card title={t('student.attendance.summary')}>
        <div className="student-attendance">
          <Ring value={pct} centerLabel={`${pct}%`} caption={t('student.attendance.percentage')} />
          <div className="student-attendance__stats">
            <Stat label={t('student.attendance.total')} value={summary.total ?? 0} />
            <Stat label={t('student.attendance.present')} value={summary.present ?? 0} />
            <Stat label={t('student.attendance.absent')} value={summary.absent ?? 0} />
            <Stat label={t('student.attendance.late')} value={summary.late ?? 0} />
            <Stat label={t('student.attendance.excused')} value={summary.excused ?? 0} />
          </div>
        </div>
        {cells.length > 0 && <div className="mt-[18px]"><Heatmap cells={cells} legend={legend} /></div>}
      </Card>
      <Card title={t('student.attendance.records')}>
        {records.length === 0 ? (
          <EmptyState title={t('student.attendance.empty')} />
        ) : (
          <ul className="ui-list">
            {records.map((item) => {
              const tone = ATTENDANCE_TONE[String(item.status || '').toLowerCase()] || 'muted'
              return (
                <li className="ui-list__item" key={item.id || item.attendanceDate}>
                  <div className="ui-list__body">
                    <div className="ui-list__title">{formatDate(item.attendanceDate, locale)}</div>
                    {(item.source || item.notes) && (
                      <div className="ui-list__meta ui-muted">{[item.source, item.notes].filter(Boolean).join(' · ')}</div>
                    )}
                  </div>
                  <Chip tone={tone} aria-label={`${t('student.attendance.status')}: ${item.status}`}>{item.status}</Chip>
                </li>
              )
            })}
          </ul>
        )}
      </Card>
    </>
  )
}

function Stat({ label, value }) {
  return (
    <div className="domain-child__stat">
      <span className="domain-child__stat-value">{value}</span>
      <span className="domain-child__stat-label">{label}</span>
    </div>
  )
}

// =============================================================================
// Communities
// =============================================================================

export default function StudentAttendancePage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <AttendancePage userId={userId} locale={locale} {...props} />
}
