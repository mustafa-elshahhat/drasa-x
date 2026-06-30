import { Spinner } from './Spinner'
import { EmptyState, ErrorState } from './states'

// Renders honest loading / error / empty states around a React Query result and
// passes the resolved data to a render-prop child. Replaces the per-portal
// `QueryResult`/`QueryCard` helpers and the bare `<p>Loading…</p>` placeholders.
//
//   <QueryBoundary query={q} emptyWhen={(d) => !d?.length} emptyTitle={...}>
//     {(data) => <List items={data} />}
//   </QueryBoundary>
export function QueryBoundary({
  query,
  children,
  loadingFallback,
  emptyWhen,
  emptyTitle,
  emptyMessage,
  emptyIcon,
}) {
  if (query.isLoading) {
    return loadingFallback ?? <Spinner label="" />
  }
  if (query.isError) {
    return <ErrorState error={query.error} onRetry={() => query.refetch()} />
  }
  const isEmpty = typeof emptyWhen === 'function' ? emptyWhen(query.data) : false
  if (isEmpty) {
    return <EmptyState title={emptyTitle} message={emptyMessage} icon={emptyIcon} />
  }
  return children(query.data)
}
