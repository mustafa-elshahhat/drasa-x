// Forbidden / Session-expired / Suspended-tenant / Not-found screens
// (Phase 7 §5). Localized, accessible, no fabricated content.
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useDocumentTitle } from '../../app/useDocumentTitle'

function StatusLayout({ titleKey, bodyKey, linkTo, linkLabelKey }) {
  const { t } = useTranslation()
  useDocumentTitle({ titleKey })
  return (
    <main className="status-page" role="main">
      <div className="status-page__card">
        <h1>{t(titleKey)}</h1>
        <p>{t(bodyKey)}</p>
        <Link className="ui-btn ui-btn--primary" to={linkTo}>
          {t(linkLabelKey)}
        </Link>
      </div>
    </main>
  )
}

export function ForbiddenPage() {
  return (
    <StatusLayout
      titleKey="pages.forbiddenTitle"
      bodyKey="pages.forbiddenBody"
      linkTo="/app"
      linkLabelKey="actions.goHome"
    />
  )
}

export function SessionExpiredPage() {
  return (
    <StatusLayout
      titleKey="pages.sessionExpiredTitle"
      bodyKey="pages.sessionExpiredBody"
      linkTo="/login"
      linkLabelKey="actions.signIn"
    />
  )
}

export function SuspendedTenantPage() {
  return (
    <StatusLayout
      titleKey="pages.suspendedTitle"
      bodyKey="pages.suspendedBody"
      linkTo="/login"
      linkLabelKey="actions.signIn"
    />
  )
}

export function NotFoundPage() {
  return (
    <StatusLayout
      titleKey="pages.notFoundTitle"
      bodyKey="pages.notFoundBody"
      linkTo="/app"
      linkLabelKey="actions.goHome"
    />
  )
}
