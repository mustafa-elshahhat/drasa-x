import { useEffect, useRef, useState } from 'react'
import type { ButtonHTMLAttributes, ComponentType, FC, ReactNode } from 'react'

// =============================================================================
// shared/ui — typed home for the DerasaX UI primitives.
//
// The implementations live in src/components/* (the prototype-aligned visual
// system). This layer adds TypeScript prop types + a single import location, and
// adds the few net-new primitives (SectionHeader, Drawer, Dropdown) styled with
// token-backed Tailwind utilities. Migrated pages import from `shared/ui`.
// =============================================================================
import { Button as RawButton } from '../../components/ui/Button'
import { Card as RawCard, PageHeader as RawPageHeader } from '../../components/ui/PageHeader'
import { Chip as RawChip } from '../../components/ui/Chip'
import { Avatar as RawAvatar } from '../../components/ui/Avatar'
import { Tabs as RawTabs } from '../../components/ui/Tabs'
import { Modal as RawModal } from '../../components/ui/Modal'
import { Alert as RawAlert } from '../../components/ui/Alert'

// Re-exported with their existing (forwardRef/required-prop) typing intact —
// these dialogs already carry specific prop contracts the migrated pages use.
export { ConfirmDialog } from '../../components/ui/ConfirmDialog'
export { FormModal } from '../../components/ui/FormModal'
import { Spinner as RawSpinner } from '../../components/ui/Spinner'
import { Skeleton as RawSkeleton, SkeletonText as RawSkeletonText } from '../../components/ui/Skeleton'
import { Toggle as RawToggle } from '../../components/ui/Toggle'
import { Stepper as RawStepper } from '../../components/ui/Stepper'
import { SearchInput as RawSearchInput } from '../../components/ui/SearchInput'
import { Toolbar as RawToolbar } from '../../components/ui/Toolbar'

/** Icon component prop shape (lucide-react icons satisfy this). */
export type IconType = ComponentType<{ size?: number | string; className?: string }>

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  // Maps to `.ui-btn--{variant}` (see components.css + legacy.css prototype variants).
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger' | 'soft' | 'outline' | 'dark'
  loading?: boolean
}
export const Button = RawButton as FC<ButtonProps>

export interface CardProps {
  title?: ReactNode
  description?: ReactNode
  className?: string
  children?: ReactNode
}
export const Card = RawCard as FC<CardProps>

export interface PageHeaderProps {
  title: ReactNode
  description?: ReactNode
  actions?: ReactNode
}
export const PageHeader = RawPageHeader as FC<PageHeaderProps>

export type ChipTone =
  | 'muted' | 'brand' | 'accent' | 'success' | 'danger' | 'warning' | 'info' | 'purple' | 'orange'
export interface BadgeProps {
  tone?: ChipTone
  icon?: IconType
  className?: string
  children?: ReactNode
}
/** Badge is the DerasaX `Chip` primitive (prototype `chip(text, tone)`). */
export const Badge = RawChip as FC<BadgeProps>
export const Chip = Badge

export interface AvatarProps {
  name?: string
  size?: number
  color?: string
  src?: string
  alt?: string
}
export const Avatar = RawAvatar as FC<AvatarProps>

export interface TabItem {
  id: string
  label: ReactNode
  count?: number
}
export interface TabsProps {
  tabs: TabItem[]
  active?: string
  onChange?: (id: string) => void
  ariaLabel?: string
}
export const Tabs = RawTabs as FC<TabsProps>

export interface DialogProps {
  open: boolean
  onClose?: () => void
  title?: ReactNode
  footer?: ReactNode
  labelledById?: string
  children?: ReactNode
}
/** Dialog is the accessible `Modal` (focus-trapped, Escape/backdrop close). */
export const Dialog = RawModal as FC<DialogProps>
export const Modal = Dialog

export interface AlertProps {
  variant?: 'info' | 'success' | 'warning' | 'error'
  title?: ReactNode
  onDismiss?: () => void
  correlationId?: string
  children?: ReactNode
}
export const Alert = RawAlert as FC<AlertProps>

export interface SpinnerProps {
  size?: number
  label?: string
  inline?: boolean
}
export const Spinner = RawSpinner as FC<SpinnerProps>

export interface SkeletonProps {
  width?: number | string
  height?: number | string
  radius?: number
  className?: string
}
export const Skeleton = RawSkeleton as FC<SkeletonProps>
export const SkeletonText = RawSkeletonText as FC<{ lines?: number }>

export interface ToggleProps {
  checked?: boolean
  onChange?: (checked: boolean) => void
  label?: ReactNode
  disabled?: boolean
  id?: string
}
/** Accessible on/off switch (prototype toggle). */
export const Toggle = RawToggle as FC<ToggleProps>

export interface StepperStep {
  label: ReactNode
  description?: ReactNode
}
export interface StepperProps {
  steps?: (StepperStep | string)[]
  current?: number
}
/** Horizontal multi-step progress indicator. */
export const Stepper = RawStepper as FC<StepperProps>

export interface SearchInputProps {
  value?: string
  onChange?: (value: string) => void
  placeholder?: string
  id?: string
  label?: ReactNode
}
/** Labelled search box (debounce/clearing handled by the caller). */
export const SearchInput = RawSearchInput as FC<SearchInputProps>

export interface ToolbarFilter {
  id: string
  label: ReactNode
  options: { value: string; label: ReactNode }[]
  value?: string
}
export interface ToolbarProps {
  search?: ReactNode
  filters?: ToolbarFilter[]
  onFilter?: (id: string, value: string) => void
  action?: ReactNode
}
/** Page toolbar: search + filter selects + a trailing action slot. */
export const Toolbar = RawToolbar as FC<ToolbarProps>

// --- Net-new typed primitives -------------------------------------------------

export interface SectionHeaderProps {
  title: ReactNode
  description?: ReactNode
  actions?: ReactNode
}
/** A lighter heading row for sections within a page (vs the page-level PageHeader). */
export function SectionHeader({ title, description, actions }: SectionHeaderProps) {
  return (
    <div className="flex items-center justify-between gap-3 mb-3">
      <div className="min-w-0">
        <h2 className="m-0 text-[1.05rem] font-bold text-ink truncate">{title}</h2>
        {description && <p className="m-0 mt-0.5 text-sm text-muted">{description}</p>}
      </div>
      {actions && <div className="flex items-center gap-2 shrink-0">{actions}</div>}
    </div>
  )
}

export interface DrawerProps {
  open: boolean
  onClose?: () => void
  title?: ReactNode
  /** Logical edge the drawer slides from (RTL-aware). */
  side?: 'start' | 'end'
  children?: ReactNode
}
/** Off-canvas panel with backdrop + Escape-to-close. Logical-edge aware (RTL). */
export function Drawer({ open, onClose, title, side = 'end', children }: DrawerProps) {
  useEffect(() => {
    if (!open) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose?.()
    }
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [open, onClose])

  if (!open) return null
  return (
    <div className="fixed inset-0 z-50">
      <div className="absolute inset-0 bg-black/40" onClick={() => onClose?.()} aria-hidden="true" />
      <aside
        className={`absolute top-0 bottom-0 ${side === 'start' ? 'start-0' : 'end-0'} w-[min(420px,90vw)] bg-surface shadow-pop p-5 overflow-y-auto`}
        role="dialog"
        aria-modal="true"
        aria-label={typeof title === 'string' ? title : undefined}
      >
        {title && <h2 className="m-0 mb-4 text-lg font-bold text-ink">{title}</h2>}
        {children}
      </aside>
    </div>
  )
}

export interface DropdownItem {
  id: string
  label: ReactNode
  onSelect?: () => void
  href?: string
  disabled?: boolean
}
export interface DropdownProps {
  trigger: ReactNode
  items: DropdownItem[]
  ariaLabel?: string
  /** Logical alignment of the menu under the trigger (RTL-aware). */
  align?: 'start' | 'end'
}
/** Menu button: outside-click + Escape close, logical alignment (RTL). */
export function Dropdown({ trigger, items, ariaLabel, align = 'end' }: DropdownProps) {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    const onDoc = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpen(false)
    }
    document.addEventListener('mousedown', onDoc)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onDoc)
      document.removeEventListener('keydown', onKey)
    }
  }, [open])

  return (
    <div className="relative inline-block" ref={ref}>
      <button
        type="button"
        className="ui-btn ui-btn--ghost"
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => setOpen((o) => !o)}
      >
        {trigger}
      </button>
      {open && (
        <div
          className={`absolute top-full mt-1 ${align === 'start' ? 'start-0' : 'end-0'} min-w-[180px] bg-surface border border-line rounded-soft shadow-pop py-1 z-50`}
          role="menu"
          aria-label={ariaLabel}
        >
          {items.map((item) =>
            item.href ? (
              <a
                key={item.id}
                href={item.href}
                role="menuitem"
                className="block px-3 py-2 text-sm text-ink no-underline hover:bg-surface-2"
              >
                {item.label}
              </a>
            ) : (
              <button
                key={item.id}
                type="button"
                role="menuitem"
                disabled={item.disabled}
                className="block w-full text-start px-3 py-2 text-sm text-ink hover:bg-surface-2 disabled:opacity-50"
                onClick={() => {
                  item.onSelect?.()
                  setOpen(false)
                }}
              >
                {item.label}
              </button>
            )
          )}
        </div>
      )}
    </div>
  )
}
