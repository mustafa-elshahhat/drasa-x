import type { FC } from 'react'

// =============================================================================
// shared/navigation — navigation primitives. `Tabs` lives in shared/ui; this
// module is the home for cross-page navigation controls like Pagination.
// =============================================================================
import { Pagination as RawPagination } from '../../components/ui/Pagination'
export { Tabs } from '../ui'

export interface PaginationProps {
  page: number
  pageCount: number
  onChange: (page: number) => void
}
/** Logical (prev/next) pagination; CSS flips the visual order under RTL. */
export const Pagination = RawPagination as FC<PaginationProps>
