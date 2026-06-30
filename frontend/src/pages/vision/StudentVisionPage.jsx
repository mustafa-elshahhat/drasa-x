import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { PageHeader, Card } from '../../shared/ui'
import { EmptyState, ErrorState } from '../../shared/feedback'
import { useAuth } from '../../features/auth/AuthContext'
import { visionApi } from '../../features/vision/visionApi'
import { queryKeys, STALE } from '../../lib/query/keys'

// Phase 15 — a student's OWN read-only CV engagement summary. No other students,
// no raw images, no face crops — only aggregated counts for the signed-in student.
export default function StudentVisionPage() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const query = useQuery({
    queryKey: queryKeys.vision.myEngagement(user?.id),
    queryFn: ({ signal }) => visionApi.myEngagementSummary(signal),
    staleTime: STALE.short,
  })

  return (
    <>
      <PageHeader title={t('vision.studentTitle')} description={t('vision.studentDescription')} />
      <p className="ui-muted">{t('vision.studentPrivacyNote')}</p>
      {query.isLoading && <p role="status">{t('states.loading', 'Loading…')}</p>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && <EngagementSummaryCard data={query.data} />}
    </>
  )
}

export function EngagementSummaryCard({ data }) {
  const { t } = useTranslation()
  const total = (data.engagedObservations ?? 0) + (data.disengagedObservations ?? 0) + (data.notReadyObservations ?? 0)
  if (total === 0 && (data.cvAttendanceCount ?? 0) === 0) {
    return <EmptyState title={t('vision.studentEmptyTitle')} message={t('vision.studentEmptyBody')} />
  }
  return (
    <Card title={t('vision.summaryTitle')}>
      {data.degraded && <p className="ui-muted">{t('vision.degradedBody')}</p>}
      <dl className="ui-kv" data-testid="cv-student-summary">
        <div><dt>{t('vision.engaged')}</dt><dd>{data.engagedObservations ?? 0}</dd></div>
        <div><dt>{t('vision.disengaged')}</dt><dd>{data.disengagedObservations ?? 0}</dd></div>
        <div><dt>{t('vision.notReady')}</dt><dd>{data.notReadyObservations ?? 0}</dd></div>
        <div><dt>{t('vision.sessionsObserved')}</dt><dd>{data.sessionsObserved ?? 0}</dd></div>
        <div><dt>{t('vision.cvAttendance')}</dt><dd>{data.cvAttendanceCount ?? 0}</dd></div>
      </dl>
    </Card>
  )
}
