import type { FC, ReactNode } from 'react'
import type { IconType } from '../ui'

// =============================================================================
// shared/data-display — tables, metric cards, and stat grids.
// =============================================================================
import { DataTable as RawDataTable } from '../../components/ui/DataTable'
import { Metric as RawMetric } from '../../components/ui/Metric'
import { DetailList as RawDetailList } from '../../components/data/DetailList'
import { ResourceTable as RawResourceTable } from '../../components/data/ResourceTable'
// Crud carries a rich required-prop contract owned by the implementation; it is
// re-exported as-is (no lossy retyping).
export { Crud } from '../../components/data/Crud'

export interface DataTableColumn<Row> {
  key: string
  header: ReactNode
  render?: (row: Row) => ReactNode
  align?: 'left' | 'right' | 'center' | 'start' | 'end'
}
export interface DataTableProps<Row> {
  columns: DataTableColumn<Row>[]
  rows?: Row[] | null
  rowKey?: (row: Row, index: number) => string | number
  loading?: boolean
  error?: unknown
  onRetry?: () => void
  emptyTitle?: ReactNode
  emptyMessage?: ReactNode
  caption?: string
}
/** Accessible table with built-in loading/empty/error states (generic rows). */
export const DataTable = RawDataTable as <Row = Record<string, unknown>>(
  props: DataTableProps<Row>
) => ReactNode

export interface MetricCardProps {
  to?: string
  icon?: IconType
  label?: ReactNode
  value?: ReactNode
  sub?: ReactNode
  /** Per-tile accent color (CSS color); defaults to the current --accent. */
  accent?: string
}
/** KPI tile (prototype `metric()`); renders as a Link when `to` is provided. */
export const MetricCard = RawMetric as FC<MetricCardProps>
export const Metric = MetricCard

/** Chip descriptor a `chip`/status mapper resolves to. */
export interface ChipDescriptor {
  tone?: string
  label?: ReactNode
  labelKey?: string
}

/** A `DetailList` field descriptor (typed/translated, never a raw API key). */
export interface DetailField {
  key?: string
  label?: ReactNode
  labelKey?: string
  value?: ReactNode
  format?: string
  accessor?: (item: unknown) => unknown
  chip?: Record<string, ChipDescriptor> | ((value: unknown, item: unknown) => ChipDescriptor | null)
  render?: (item: unknown) => ReactNode
}
export interface DetailListProps {
  item: unknown
  fields?: DetailField[]
  locale?: string
  autoOptions?: { include?: string[]; exclude?: string[] }
}
/** Typed key/value detail grid (translated labels, honest humanized fallback). */
export const DetailList = RawDetailList as FC<DetailListProps>

/** A `ResourceTable` column descriptor (typed/translated headers). */
export interface ResourceColumn<Row = unknown> {
  key: string
  header?: ReactNode
  headerKey?: string
  align?: 'left' | 'right' | 'center' | 'start' | 'end'
  format?: string
  kind?: 'bool' | 'status' | 'role'
  accessor?: (row: Row) => unknown
  bool?: { trueKey?: string; falseKey?: string; trueTone?: string; falseTone?: string }
  chip?: Record<string, ChipDescriptor> | ((value: unknown, row: Row) => ChipDescriptor | null)
  render?: (row: Row) => ReactNode
}
export interface ResourceTableProps<Row = Record<string, unknown>> {
  rows?: Row[] | null
  columns?: ResourceColumn<Row>[]
  loading?: boolean
  error?: unknown
  onRetry?: () => void
  rowKey?: (row: Row, index: number) => string | number
  emptyTitle?: ReactNode
  emptyMessage?: ReactNode
  caption?: string
  locale?: string
  /** Appended as a trailing `__actions` column (mirrors `Crud`'s row-actions contract). */
  rowActions?: (row: Row) => ReactNode
}
/** Binds records to the responsive DataTable via typed, translated columns. */
export const ResourceTable = RawResourceTable as <Row = Record<string, unknown>>(
  props: ResourceTableProps<Row>
) => ReactNode

export interface StatGridProps {
  children: ReactNode
  className?: string
}
/** Responsive auto-fill grid for metric cards (matches the prototype dashboards). */
export function StatGrid({ children, className = '' }: StatGridProps) {
  return (
    <div
      className={`grid gap-3 [grid-template-columns:repeat(auto-fill,minmax(220px,1fr))] ${className}`.trim()}
    >
      {children}
    </div>
  )
}
