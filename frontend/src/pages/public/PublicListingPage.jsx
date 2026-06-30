import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useDocumentTitle } from '../../app/useDocumentTitle'
import '../../styles/public-pages.css'

// Shared marketing listing page (Events / Activities / News). The platform does
// not expose a public content feed yet, so this renders the prototype listing
// container with an honest empty state — no fabricated cards or dates.
export function PublicListingPage({ titleKey, bodyKey, icon: Icon }) {
  const { t } = useTranslation()
  useDocumentTitle({ titleKey })
  return (
    <main className="public-page" role="main">
      <header className="public-page__header">
        <h1 className="public-page__title">{t(titleKey)}</h1>
        <p className="public-page__body">{t(bodyKey)}</p>
      </header>
      <div className="public-page__empty">
        {Icon && (
          <span className="public-page__empty-icon" aria-hidden="true">
            <Icon size={34} />
          </span>
        )}
        <h2 className="public-page__empty-title">{t('public.listing.emptyTitle')}</h2>
        <p className="public-page__empty-body">{t('public.listing.emptyBody')}</p>
        <Link to="/request-demo" className="public-page__cta">{t('public.nav.requestDemo')}</Link>
      </div>
    </main>
  )
}
