import { ResponsiveContainer } from 'recharts'
import { Spinner } from './Spinner'
import { EmptyState, ErrorState } from './states'

// Charts wrapper (Phase 7 §10). Standardizes loading/empty/error around any
// recharts chart and provides an accessible text summary alternative so the
// data is not conveyed by color/visuals alone.
export function ChartWrapper({ title, summary, loading, error, onRetry, isEmpty, height = 300, children }) {
  if (error) return <ErrorState error={error} onRetry={onRetry} />
  if (loading)
    return (
      <div className="ui-chart" style={{ height }}>
        <Spinner label="Loading chart" />
      </div>
    )
  if (isEmpty) return <EmptyState title={title} />
  return (
    <figure className="ui-chart" style={{ height }}>
      <ResponsiveContainer width="100%" height="100%">
        {children}
      </ResponsiveContainer>
      {summary && <figcaption className="ui-visually-hidden">{summary}</figcaption>}
    </figure>
  )
}
