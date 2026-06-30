import type { FC, ReactNode } from 'react'
import type { IconType } from '../ui'

// =============================================================================
// shared/data-display — tables, metric cards, and stat grids.
// =============================================================================
import { DataTable as RawDataTable } from '../../components/ui/DataTable'
import { Metric as RawMetric } from '../../components/ui/Metric'

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
