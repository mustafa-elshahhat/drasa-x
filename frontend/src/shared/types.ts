// =============================================================================
// shared/types — canonical cross-cutting TypeScript types for the DerasaX
// frontend boundaries (route metadata, roles/permissions, API envelopes,
// select/metric/table models, query state).
//
// These describe SHAPES the app already uses at runtime; they are intentionally
// conservative. Where an API payload is uncertain, the generic stays `unknown`
// rather than inventing a precise shape (per the Phase 13.4 rule: do not fake
// types that hide real API shapes — refine them in the per-feature mappers).
// =============================================================================
import type { ReactNode } from 'react'

// --- Roles & permissions (single source of truth: features/auth/roles.ts) ----
export type { Role, Permission } from '../features/auth/roles'

// --- Route metadata ----------------------------------------------------------
export type { AppRoute, RouteComponent, RouteLayout } from '../app/router/route.types'
export type { AppRoute as RouteMeta } from '../app/router/route.types'

// --- API envelopes -----------------------------------------------------------
/**
 * A page of records as returned by the DerasaX list endpoints. The backend uses
 * `items` + `pageSize` (+ `totalCount` on most list reads); the optional fields
 * stay optional because not every endpoint returns the full envelope.
 */
export interface PagedResult<T> {
  items: T[]
  totalCount?: number
  pageSize?: number
  pageNumber?: number
}

/**
 * A discriminated success/failure envelope for frontend mappers and hooks that
 * choose to normalize a throwing call into a value. `error` is `unknown` (the API
 * client throws `ApiError`, but callers should narrow it themselves).
 */
export type ApiResult<T> =
  | { ok: true; data: T }
  | { ok: false; error: unknown }

// --- Form / select models ----------------------------------------------------
/** An `<option>` for `SelectField` / `RadioGroup` (shape: `{ value, label }`). */
export interface SelectOption {
  value: string | number
  label: ReactNode
  disabled?: boolean
}

// --- Data-display models -----------------------------------------------------
/** Re-export the table column descriptor under the canonical `TableColumn` name. */
export type { DataTableColumn as TableColumn } from './data-display'

/** A KPI/stat tile model (matches `MetricCard` props). */
export interface MetricItem {
  label: ReactNode
  value: ReactNode
  sub?: ReactNode
  /** Per-tile accent (CSS color); defaults to the current `--accent`. */
  accent?: string
  /** When set, the tile renders as a Link to this route. */
  to?: string
}

// --- Query state -------------------------------------------------------------
/** The minimal React-Query result shape the shared `QueryState` consumes. */
export interface QueryState<T> {
  data?: T
  isLoading: boolean
  isError: boolean
  error?: unknown
  refetch: () => unknown
}

// --- Re-exported shared UI prop types (single import entry) -------------------
export type {
  ButtonProps,
  CardProps,
  PageHeaderProps,
  BadgeProps,
  AvatarProps,
  TabsProps,
  DialogProps,
  AlertProps,
  ToggleProps,
  StepperProps,
} from './ui'
export type { DataTableProps, MetricCardProps, DetailListProps, ResourceTableProps } from './data-display'
export type { EmptyStateProps, ErrorStateProps, LoadingStateProps, NotEnoughDataProps } from './feedback'
export type { FormFieldProps } from './form'
export type { PageShellProps, PageSectionProps } from './layout'
