import { useQuery } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { PageHeader } from '../../shared/ui'
import { ErrorState } from '../../shared/feedback'
import { useAuth } from '../../features/auth/AuthContext'
import { visionApi } from '../../features/vision/visionApi'
import { queryKeys, STALE } from '../../lib/query/keys'
import { EngagementSummaryCard } from './StudentVisionPage'

// Phase 15 — a parent's read-only CV engagement summary for a LINKED child only.
// Access is relationship-authorized server-side; an unlinked/cross-tenant child
// returns 403/404 and is shown as an honest error (never another student's data).
export default function ParentVisionPage() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const { childId } = useParams()
  const query = useQuery({
    queryKey: queryKeys.vision.childEngagement(user?.id, childId),
    queryFn: ({ signal }) => visionApi.childEngagementSummary(childId, signal),
    staleTime: STALE.short,
    enabled: Boolean(childId),
  })

  return (
    <>
      <PageHeader title={t('vision.parentTitle')} description={t('vision.parentDescription')} />
      <p className="ui-muted">{t('vision.parentPrivacyNote')}</p>
      {query.isLoading && <p role="status">{t('states.loading', 'Loading…')}</p>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && <EngagementSummaryCard data={query.data} />}
    </>
  )
}
