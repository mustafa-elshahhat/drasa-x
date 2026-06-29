import { Spinner } from './Spinner'

// Standard button (Phase 7 §10). Variants + a loading state that disables the
// button and prevents duplicate submissions.
export function Button({
  variant = 'primary',
  type = 'button',
  loading = false,
  disabled = false,
  children,
  className = '',
  ...rest
}) {
  return (
    <button
      type={type}
      className={`ui-btn ui-btn--${variant} ${className}`}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      {...rest}
    >
      {loading && <Spinner size={16} inline label="" />}
      <span>{children}</span>
    </button>
  )
}
