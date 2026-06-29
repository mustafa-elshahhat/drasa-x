import { useState, useCallback } from 'react'
import { Outlet, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../features/auth/AuthContext'
import { Sidebar } from './Sidebar'
import { Header } from './Header'
import { Breadcrumbs } from './Breadcrumbs'

// Authenticated application shell (Phase 7 §6). Provides:
//   * a skip link to main content,
//   * a persistent sidebar on desktop and a toggleable drawer on mobile,
//   * a header with user/role context, language switch, and sign-out,
//   * breadcrumbs + a page container that lazy-loaded routes render into.
export function AppShell() {
  const { user, role, logout } = useAuth()
  const navigate = useNavigate()
  const { t } = useTranslation()
  const [navOpen, setNavOpen] = useState(false)

  const closeNav = useCallback(() => setNavOpen(false), [])

  const onSignOut = useCallback(async () => {
    await logout()
    navigate('/login', { replace: true })
  }, [logout, navigate])

  return (
    <div className={`app-shell${navOpen ? ' app-shell--nav-open' : ''}`}>
      <a className="app-skip-link" href="#main-content">
        {t('nav.skipToContent', 'Skip to main content')}
      </a>

      {/* Mobile drawer backdrop */}
      {navOpen && <div className="app-shell__backdrop" onClick={closeNav} aria-hidden="true" />}

      <aside className="app-shell__nav">
        <Sidebar role={role} onNavigate={closeNav} />
      </aside>

      <div className="app-shell__main">
        <Header user={user} role={role} onToggleNav={() => setNavOpen((o) => !o)} onSignOut={onSignOut} />
        <main id="main-content" className="app-shell__content" tabIndex={-1}>
          <Breadcrumbs />
          <Outlet />
        </main>
      </div>
    </div>
  )
}
