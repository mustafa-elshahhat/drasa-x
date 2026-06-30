// =============================================================================
// shared — DerasaX shared UI foundation. Typed primitives backed by the existing
// prototype-aligned components (src/components/*) plus a few net-new primitives.
// Import from the category (`shared/ui`, `shared/feedback`, ...) or this root.
//
// `Pagination` is re-exported from navigation only (it would otherwise collide
// with the `Tabs` re-export); `Tabs` comes from `shared/ui`.
// =============================================================================
export * from './ui'
export * from './form'
export * from './data-display'
export * from './feedback'
export * from './layout'
export { Pagination } from './navigation'
