import type { FC, ReactNode } from 'react'
import { Spinner, type IconType } from '../ui'

// =============================================================================
// shared/feedback — honest loading / empty / error / query states.
// =============================================================================
import { ErrorState as RawErrorState, EmptyState as RawEmptyState } from '../../components/ui/states'
import { QueryBoundary as RawQueryBoundary } from '../../components/ui/QueryBoundary'
import { NotEnoughData as RawNotEnoughData } from '../../components/ui/NotEnoughData'
import { FullPageLoader as RawFullPageLoader } from '../../components/ui/FullPageLoader'

// The app-level toast provider + `useToast` hook are exposed from the sibling
// non-component module `shared/feedback/toast` — re-exporting the hook from this
// JSX-bearing barrel would trip react-refresh's only-export-components rule.

export interface LoadingStateProps {
  label?: string
  inline?: boolean
  size?: number
}
/** Unified loading indicator: a spinner with an optional visible label. */
export function LoadingState({ label, inline = false, size = 24 }: LoadingStateProps) {
  return (
    <div className={inline ? 'inline-flex items-center gap-2' : 'flex items-center justify-center gap-2 py-8'}>
      {/* When a visible label is shown, keep the spinner's own a11y label empty
          to avoid announcing the text twice; otherwise give it a default name. */}
      <Spinner size={size} label={label ? '' : 'Loading'} inline={inline} />
      {label && <span className="text-sm text-muted">{label}</span>}
    </div>
  )
}

export interface ErrorStateProps {
  error?: unknown
  onRetry?: () => void
  title?: ReactNode
}
export const ErrorState = RawErrorState as FC<ErrorStateProps>

export interface EmptyStateProps {
  title?: ReactNode
  message?: ReactNode
  icon?: IconType
  action?: ReactNode
}
export const EmptyState = RawEmptyState as FC<EmptyStateProps>

/** Minimal structural view of a React Query result that QueryState consumes. */
export interface QueryLike<T> {
  isLoading: boolean
  isError: boolean
  error?: unknown
  data?: T
  refetch: () => unknown
}
export interface QueryStateProps<T> {
  query: QueryLike<T>
  children: (data: T) => ReactNode
  loadingFallback?: ReactNode
  emptyWhen?: (data: T | undefined) => boolean
  emptyTitle?: ReactNode
  emptyMessage?: ReactNode
  emptyIcon?: IconType
}
/**
 * Renders honest loading/error/empty states around a query and passes resolved
 * data to a render-prop child. (DerasaX `QueryBoundary` with typed props.)
 */
export const QueryState = RawQueryBoundary as <T = unknown>(props: QueryStateProps<T>) => ReactNode
/** Back-compat alias: `QueryBoundary` is the original name for `QueryState`. */
export const QueryBoundary = QueryState

export interface NotEnoughDataProps {
  title?: ReactNode
  message?: ReactNode
  icon?: IconType
  compact?: boolean
}
/** Honest "not enough data yet" placeholder for charts/widgets with sparse data. */
export const NotEnoughData = RawNotEnoughData as FC<NotEnoughDataProps>

export interface FullPageLoaderProps {
  label?: string
}
/** Full-viewport loading state (route-level Suspense fallback). */
export const FullPageLoader = RawFullPageLoader as FC<FullPageLoaderProps>
