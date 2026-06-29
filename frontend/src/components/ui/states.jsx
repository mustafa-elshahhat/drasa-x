import { Inbox, TriangleAlert, RefreshCw } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { userMessageFor } from '../../lib/api/problemDetails'

// Empty + error display states (Phase 7 §12). Every data view should render an
// intentional empty and error state rather than a blank screen or raw error.

export function EmptyState({ title, message, icon, action }) {
  const { t } = useTranslation()
  const Icon = icon || Inbox
  return (
    <div className="ui-state ui-state--empty" role="status">
      <Icon className="ui-state__icon" size={40} aria-hidden="true" />
      <h3 className="ui-state__title">{title || t('states.emptyTitle', 'Nothing here yet')}</h3>
      {message && <p className="ui-state__msg">{message}</p>}
      {action}
    </div>
  )
}

/**
 * Error state with a safe, user-facing message and (when available) the support
 * correlation ID. Never renders stack traces or internal details.
 */
export function ErrorState({ error, onRetry, title }) {
  const { t } = useTranslation()
  const message = userMessageFor(error, t)
  const correlationId = error?.correlationId
  return (
    <div className="ui-state ui-state--error" role="alert">
      <TriangleAlert className="ui-state__icon" size={40} aria-hidden="true" />
      <h3 className="ui-state__title">{title || t('states.errorTitle', 'Something went wrong')}</h3>
      <p className="ui-state__msg">{message}</p>
      {correlationId && (
        <p className="ui-state__meta">
          {t('errors.referenceId', 'Reference ID')}: <code>{correlationId}</code>
        </p>
      )}
      {onRetry && (
        <button type="button" className="ui-btn ui-btn--secondary" onClick={onRetry}>
          <RefreshCw size={16} aria-hidden="true" /> {t('actions.retry', 'Try again')}
        </button>
      )}
    </div>
  )
}
