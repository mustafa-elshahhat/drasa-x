import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { GraduationCap } from 'lucide-react'
import { LanguageSwitcher } from './LanguageSwitcher'
import '../styles/public-navbar.css'

// Public marketing top bar. DerasaX text wordmark (matching the portal's
// GraduationCap brand), i18n links, and the shared LanguageSwitcher so the
// marketing pages get the same en/ar + RTL behavior as the portal. "Sign in"
// routes to the real in-app /login; "Request a demo" to /request-demo.
export function PublicNavBar() {
  const { t } = useTranslation()
  return (
    <div className="public-nav__container">
      <nav className="public-nav" aria-label={t('public.nav.home')}>
        <Link to="/" className="public-nav__brand" aria-label={t('app.name')}>
          <GraduationCap size={32} aria-hidden="true" className="public-nav__brand-icon" />
          <span className="public-nav__brand-text">
            <span className="public-nav__brand-name">{t('app.name')}</span>
            <span className="public-nav__brand-tagline">{t('public.nav.tagline')}</span>
          </span>
        </Link>

        <div className="public-nav__links">
          <Link to="/">{t('public.nav.home')}</Link>
          <a href="#about">{t('public.nav.about')}</a>
          <a href="#product">{t('public.nav.product')}</a>
        </div>

        <div className="public-nav__actions">
          <Link to="/request-demo" className="public-btn public-btn--demo">
            {t('public.nav.requestDemo')}
          </Link>
          <Link to="/login" className="public-btn public-btn--signin">
            {t('public.nav.signIn')}
          </Link>
          <LanguageSwitcher />
        </div>
      </nav>
    </div>
  )
}
