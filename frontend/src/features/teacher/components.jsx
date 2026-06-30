import { useTranslation } from 'react-i18next'
import { Spinner } from '../../components/ui/Spinner'
import { ErrorState, EmptyState } from '../../components/ui/states'

// Shared teacher-portal presentational helpers (components only).

export function Loading() {
  const { t } = useTranslation()
  return <Spinner label={t('states.loading')} />
}

/** Loading/error/empty boundary returning the resolved array to its children. */
export function Listing({ query, empty, emptyIcon, children }) {
  if (query.isLoading) return <Loading />
  if (query.isError) return <ErrorState error={query.error} onRetry={() => query.refetch()} />
  const items = Array.isArray(query.data) ? query.data : []
  if (!items.length) return <EmptyState icon={emptyIcon} title={empty} />
  return children(items)
}
