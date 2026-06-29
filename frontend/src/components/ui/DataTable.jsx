import { useTranslation } from 'react-i18next'
import { Skeleton } from './Skeleton'
import { EmptyState, ErrorState } from './states'

// Reusable, accessible data table (Phase 7 §10) with built-in loading / empty /
// error states. On narrow screens it scrolls horizontally inside a labelled
// region rather than overflowing the page (Phase 7 §16).
//
// columns: [{ key, header, render?(row), align? }]
export function DataTable({
  columns,
  rows,
  rowKey = (r, i) => r.id ?? i,
  loading = false,
  error = null,
  onRetry,
  emptyTitle,
  emptyMessage,
  caption,
}) {
  const { t } = useTranslation()

  if (error) return <ErrorState error={error} onRetry={onRetry} />

  if (loading) {
    return (
      <div className="ui-table-wrap" aria-busy="true">
        <span className="ui-visually-hidden" role="status">
          {t('states.loading', 'Loading…')}
        </span>
        <table className="ui-table">
          <thead>
            <tr>
              {columns.map((c) => (
                <th key={c.key} scope="col">
                  {c.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {Array.from({ length: 5 }).map((_, r) => (
              <tr key={r}>
                {columns.map((c) => (
                  <td key={c.key}>
                    <Skeleton />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    )
  }

  if (!rows || rows.length === 0) {
    return <EmptyState title={emptyTitle} message={emptyMessage} />
  }

  return (
    <div className="ui-table-wrap" role="region" aria-label={caption || t('table.label', 'Data table')} tabIndex={0}>
      <table className="ui-table">
        {caption && <caption className="ui-visually-hidden">{caption}</caption>}
        <thead>
          <tr>
            {columns.map((c) => (
              <th key={c.key} scope="col" style={c.align ? { textAlign: c.align } : undefined}>
                {c.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, i) => (
            <tr key={rowKey(row, i)}>
              {columns.map((c) => (
                <td key={c.key} style={c.align ? { textAlign: c.align } : undefined}>
                  {c.render ? c.render(row) : row[c.key]}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
