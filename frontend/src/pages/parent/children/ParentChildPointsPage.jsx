import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Trophy } from 'lucide-react'
import { Chip } from '../../../components/ui/Chip'
import { Metric } from '../../../components/ui/Metric'
import { Card, PageHeader } from '../../../components/ui/PageHeader'
import { EmptyState, ErrorState } from '../../../components/ui/states'
import { Loading } from '../../../features/parent/components'
import { useParentQuery } from '../../../features/parent/helpers'
import { parentApi } from '../../../features/parent/parentApi'
import { displayValue, formatDate, itemId } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function ChildPointsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { childId } = useParams()
  const summary = useParentQuery(queryKeys.parent.childPoints(userId, childId), (signal) => parentApi.childPoints(childId, signal))
  const ledger = useParentQuery([...queryKeys.parent.childPoints(userId, childId), 'ledger'], (signal) => parentApi.childPointsLedger(childId, signal))
  const total = summary.data?.totalPoints ?? summary.data?.TotalPoints ?? 0
  return (
    <>
      <PageHeader title={t('parent.points.title')} description={t('parent.points.description')} />
      {summary.isLoading && <Loading />}
      {summary.isError && <ErrorState error={summary.error} onRetry={summary.refetch} />}
      {summary.data && (
        <div className="student-dashboard">
          <Metric icon={Trophy} accent="var(--purple)" label={t('parent.points.total')} value={total} />
        </div>
      )}
      <Card title={t('parent.points.ledger')}>
        {ledger.isError ? <ErrorState error={ledger.error} onRetry={ledger.refetch} />
          : (ledger.data?.length ?? 0) === 0 ? <EmptyState title={t('parent.points.empty')} />
          : (
            <ul className="ui-list">
              {(ledger.data || []).map((item, idx) => (
                <li className="ui-list__item" key={itemId(item) || idx}>
                  <div className="ui-list__body">
                    <div className="ui-list__title">{displayValue(item, ['reason', 'Reason']) || displayValue(item)}</div>
                    <div className="ui-list__meta ui-muted">{formatDate(item.createdAt ?? item.CreatedAt, locale)}</div>
                  </div>
                  <Chip tone="brand">{item.points ?? item.Points}</Chip>
                </li>
              ))}
            </ul>
          )}
      </Card>
    </>
  )
}

export default function ParentChildPointsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <ChildPointsPage userId={userId} locale={locale} {...props} />
}
