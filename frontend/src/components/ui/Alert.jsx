import { AlertCircle, CheckCircle2, Info, TriangleAlert, X } from 'lucide-react'

// Inline alert/banner (Phase 7 §10). Error/warning variants use role="alert"
// (assertive); info/success use role="status" (polite).
const ICONS = { info: Info, success: CheckCircle2, warning: TriangleAlert, error: AlertCircle }

export function Alert({ variant = 'info', title, children, onDismiss, correlationId }) {
  const Icon = ICONS[variant] || Info
  const assertive = variant === 'error' || variant === 'warning'
  return (
    <div
      className={`ui-alert ui-alert--${variant}`}
      role={assertive ? 'alert' : 'status'}
      aria-live={assertive ? 'assertive' : 'polite'}
    >
      <Icon className="ui-alert__icon" size={20} aria-hidden="true" />
      <div className="ui-alert__body">
        {title && <p className="ui-alert__title">{title}</p>}
        {children && <div className="ui-alert__text">{children}</div>}
        {correlationId && (
          <p className="ui-alert__meta">
            Reference ID: <code>{correlationId}</code>
          </p>
        )}
      </div>
      {onDismiss && (
        <button type="button" className="ui-alert__close" onClick={onDismiss} aria-label="Dismiss">
          <X size={16} aria-hidden="true" />
        </button>
      )}
    </div>
  )
}
