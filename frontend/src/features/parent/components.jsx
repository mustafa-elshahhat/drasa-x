import { useTranslation } from 'react-i18next'
import { Spinner } from '../../shared/ui'
import { ErrorState, EmptyState } from '../../shared/feedback'

// Shared parent-portal presentational helpers (components only).

export function Loading() {
  const { t } = useTranslation()
  return <Spinner label={t('states.loading')} />
}

export function Stat({ label, value }) {
  return (
    <div className="domain-child__stat">
      <span className="domain-child__stat-value">{value ?? '—'}</span>
      <span className="domain-child__stat-label">{label}</span>
    </div>
  )
}

/** Loading/error/empty boundary returning the resolved array to its children. */
export function Listing({ query, empty, emptyIcon, children }) {
  if (query.isLoading) return <Loading />
  if (query.isError) return <ErrorState error={query.error} onRetry={() => query.refetch()} />
  const items = Array.isArray(query.data) ? query.data : []
  if (!items.length) return <EmptyState icon={emptyIcon} title={empty} />
  return children(items)
}
