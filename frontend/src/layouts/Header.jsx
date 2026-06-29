import { useTranslation } from 'react-i18next'
import { Menu, LogOut } from 'lucide-react'
import { LanguageSwitcher } from './LanguageSwitcher'
import { NotificationBell } from '../features/notifications/NotificationBell'

// Application header (Phase 7 §6). Shows the mobile menu toggle, current user +
// role context, language switcher, and sign-out.
export function Header({ user, role, onToggleNav, onSignOut }) {
  const { t } = useTranslation()
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

      <div className="app-header__spacer" />

      <div className="app-header__user">
        <div className="app-header__user-text">
          <span className="app-header__user-name">{user?.fullName || user?.userName}</span>
          {role && <span className="app-header__user-role">{t(`roles.${role}`, role)}</span>}
        </div>
      </div>

      <NotificationBell />

      <LanguageSwitcher />

      <button type="button" className="app-header__signout" onClick={onSignOut}>
        <LogOut size={16} aria-hidden="true" />
        <span>{t('actions.signOut', 'Sign out')}</span>
      </button>
    </header>
  )
}
