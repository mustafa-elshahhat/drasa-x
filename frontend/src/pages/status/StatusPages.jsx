// Forbidden / Session-expired / Suspended-tenant / Not-found screens
// (Phase 7 §5). Localized, accessible, no fabricated content.
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Lock, Clock, TriangleAlert, Compass } from 'lucide-react'
import { useDocumentTitle } from '../../app/useDocumentTitle'

function StatusLayout({ icon, tone = 'brand', titleKey, bodyKey, linkTo, linkLabelKey }) {
  const { t } = useTranslation()
  const Icon = icon
  useDocumentTitle({ titleKey })
  return (
    <main className="status-page" role="main">
      <div className="status-page__card">
        <span className={`status-page__icon status-page__icon--${tone}`}>
          <Icon size={42} aria-hidden="true" />
        </span>
        <h1 className="status-page__title">{t(titleKey)}</h1>
        <p className="status-page__body">{t(bodyKey)}</p>
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
      icon={Lock}
      tone="warning"
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
      icon={Clock}
      tone="info"
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
      icon={TriangleAlert}
      tone="danger"
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
      icon={Compass}
      tone="brand"
      titleKey="pages.notFoundTitle"
      bodyKey="pages.notFoundBody"
      linkTo="/app"
      linkLabelKey="actions.goHome"
    />
  )
}
