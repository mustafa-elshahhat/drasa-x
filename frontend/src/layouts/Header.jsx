import { useEffect, useId, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Menu, LogOut, GraduationCap, ChevronDown, UserCircle, Settings, KeyRound, Search } from 'lucide-react'
import { LanguageSwitcher } from './LanguageSwitcher'
import { NotificationBell } from '../features/notifications/NotificationBell'
import { Avatar } from '../components/ui/Avatar'

// Per-role URL slug used to build the role-scoped settings destination.
const ROLE_SLUG = { Student: 'student', Teacher: 'teacher', Parent: 'parent', SchoolAdmin: 'school', SystemAdmin: 'system' }

// Application header (Phase 7 §6; prototype-parity rework). Matches the prototype
// topbar: brand wordmark, language switch, notification bell, and an account
// dropdown that holds Profile / Settings / Security / Sign out.
export function Header({ user, role, onToggleNav, onSignOut }) {
  const { t } = useTranslation()
  const [menuOpen, setMenuOpen] = useState(false)
  const accountRef = useRef(null)
  const menuId = useId()

  // Close the account menu on outside click or Escape (keyboard + pointer parity).
  useEffect(() => {
    if (!menuOpen) return undefined
    const onPointer = (e) => {
      if (accountRef.current && !accountRef.current.contains(e.target)) setMenuOpen(false)
    }
    const onKey = (e) => {
      if (e.key === 'Escape') setMenuOpen(false)
    }
    document.addEventListener('mousedown', onPointer)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onPointer)
      document.removeEventListener('keydown', onKey)
    }
  }, [menuOpen])

  const name = user?.fullName || user?.userName
  const settingsPath = ROLE_SLUG[role] ? `/app/${ROLE_SLUG[role]}/settings` : null

  return (
    <header className="app-header">
      <button
        type="button"
        className="app-header__menu-btn"
        onClick={onToggleNav}
        aria-label={t('nav.menu', 'Menu')}
      >
        <Menu size={20} aria-hidden="true" />
      </button>

      <Link to="/app" className="app-header__brand" aria-label={t('app.name', 'DerasaX')}>
        <span className="app-header__brand-mark" aria-hidden="true">
          <GraduationCap size={20} />
        </span>
        <span className="app-header__brand-word">
          Derasa<span className="app-header__brand-x">X</span>
        </span>
      </Link>

      {role === 'Student' && (
        <div className="app-header__search">
          <Search size={18} aria-hidden="true" className="app-header__search-icon" />
          <input
            type="search"
            placeholder={t('nav.searchAnything', 'Search anything…')}
            className="app-header__search-input"
          />
        </div>
      )}

      <div className="app-header__spacer" />

      <LanguageSwitcher />

      <NotificationBell />

      <div className="app-header__account" ref={accountRef}>
        <button
          type="button"
          className="app-header__account-btn"
          onClick={() => setMenuOpen((o) => !o)}
          aria-haspopup="menu"
          aria-expanded={menuOpen}
          aria-controls={menuId}
          aria-label={t('nav.account', 'Account menu')}
        >
          <Avatar name={name} size={34} />
          <span className="app-header__user-text">
            <span className="app-header__user-name">{name}</span>
            {role && <span className="app-header__user-role">{t(`roles.${role}`, role)}</span>}
          </span>
          <ChevronDown size={16} aria-hidden="true" className="app-header__account-chevron" />
        </button>

        {menuOpen && (
          <div className="app-header__menu" id={menuId} role="menu">
            <Link to="/app/profile" className="app-header__menu-item" role="menuitem" onClick={() => setMenuOpen(false)}>
              <UserCircle size={17} aria-hidden="true" /> {t('nav.profile', 'Profile')}
            </Link>
            {settingsPath && (
              <Link to={settingsPath} className="app-header__menu-item" role="menuitem" onClick={() => setMenuOpen(false)}>
                <Settings size={17} aria-hidden="true" /> {t('nav.settings', 'Settings')}
              </Link>
            )}
            <Link to="/app/security" className="app-header__menu-item" role="menuitem" onClick={() => setMenuOpen(false)}>
              <KeyRound size={17} aria-hidden="true" /> {t('nav.security', 'Security')}
            </Link>
            <div className="app-header__menu-sep" role="separator" />
            <button
              type="button"
              className="app-header__menu-item app-header__menu-item--danger"
              role="menuitem"
              onClick={() => {
                setMenuOpen(false)
                onSignOut()
              }}
            >
              <LogOut size={17} aria-hidden="true" /> {t('actions.signOut', 'Sign out')}
            </button>
          </div>
        )}
      </div>
    </header>
  )
}
