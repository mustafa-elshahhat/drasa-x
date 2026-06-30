import type { ReactNode } from 'react'
import { PageHeader, SectionHeader } from '../ui'

// =============================================================================
// shared/layout — page-level scaffolding for migrated route modules.
// =============================================================================

export interface PageShellProps {
  title?: ReactNode
  description?: ReactNode
  actions?: ReactNode
  children: ReactNode
  className?: string
}
/** Standard page wrapper: an optional PageHeader over vertically-stacked content. */
export function PageShell({ title, description, actions, children, className = '' }: PageShellProps) {
  return (
    <div className={`flex flex-col gap-5 ${className}`.trim()}>
      {title && <PageHeader title={title} description={description} actions={actions} />}
      {children}
    </div>
  )
}

export interface PageSectionProps {
  title?: ReactNode
  description?: ReactNode
  actions?: ReactNode
  children: ReactNode
  className?: string
}
/** A titled section within a page (optional SectionHeader over its content). */
export function PageSection({ title, description, actions, children, className = '' }: PageSectionProps) {
  return (
    <section className={className}>
      {title && <SectionHeader title={title} description={description} actions={actions} />}
      {children}
    </section>
  )
}
