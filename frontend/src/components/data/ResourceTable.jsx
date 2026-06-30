import { useTranslation } from 'react-i18next'
import { DataTable } from '../ui/DataTable'
import { Chip } from '../ui/Chip'
import { getField, formatField, resolveStatus, autoColumns, genericStatusTone } from '../../features/student/studentUtils'

// Binds a list of records to the responsive DataTable using typed, translated
// columns (never raw API keys). Column shapes:
//   { key, headerKey | header, format?, align? }                       // value
//   { key, headerKey, chip: { value: { tone, labelKey } } | fn }        // enum → Chip
//   { key, headerKey, bool: { trueKey, falseKey, trueTone, falseTone }} // boolean → Chip
//   { key, headerKey, render: (row) => node }                           // custom
//   { key, header, kind: 'bool'|'status'|'role' }                       // auto-derived
// When `columns` is omitted, a curated set is derived from the rows with
// humanized headers (an honest fallback for untyped admin lists, never raw keys).
export function ResourceTable({
  rows,
  columns,
  loading,
  error,
  onRetry,
  rowKey,
  emptyTitle,
  emptyMessage,
  caption,
  locale,
}) {
  const { t } = useTranslation()
  const resolvedColumns = columns && columns.length ? columns : autoColumns(rows)

  const tableColumns = resolvedColumns.map((col) => {
    const header = col.headerKey ? t(col.headerKey) : col.header
    return {
      key: col.key,
      header,
      align: col.align,
      render: (row) => {
        if (col.render) return col.render(row)
        const value = col.accessor ? col.accessor(row) : getField(row, col.key)

        if (col.bool || col.kind === 'bool') {
          const on = !!value
          const trueKey = col.bool?.trueKey || 'common.yes'
          const falseKey = col.bool?.falseKey || 'common.no'
          return (
            <Chip tone={on ? col.bool?.trueTone || 'success' : col.bool?.falseTone || 'muted'}>
              {t(on ? trueKey : falseKey)}
            </Chip>
          )
        }
        if (col.chip) {
          const desc = typeof col.chip === 'function' ? col.chip(value, row) : resolveStatus(value, col.chip)
          if (!desc) return formatField(value, col.format, { locale })
          return <Chip tone={desc.tone || 'muted'}>{desc.labelKey ? t(desc.labelKey) : desc.label}</Chip>
        }
        if (col.kind === 'status' || col.kind === 'role') {
          if (value === undefined || value === null || value === '') return '—'
          return <Chip tone={genericStatusTone(value)}>{String(value)}</Chip>
        }
        return formatField(value, col.format, { locale })
      },
    }
  })

  return (
    <DataTable
      columns={tableColumns}
      rows={rows}
      loading={loading}
      error={error}
      onRetry={onRetry}
      rowKey={rowKey}
      emptyTitle={emptyTitle}
      emptyMessage={emptyMessage}
      caption={caption}
    />
  )
}
