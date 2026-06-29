import { NavLink } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { GraduationCap } from 'lucide-react'
import { navItemsForRole } from '../navigation/navConfig'

// Sidebar navigation (Phase 7 §6/§7). Shows only destinations permitted for the
// current role. Keyboard-accessible (native links), with active-route state via
// NavLink's aria-current.
export function Sidebar({ role, onNavigate }) {
  const { t } = useTranslation()
  const items = navItemsForRole(role)

  return (
    <nav className="app-sidebar" aria-label={t('nav.mainNavigation', 'Main navigation')}>
      <div className="app-sidebar__brand">
        <GraduationCap size={22} aria-hidden="true" />
        <span>{t('app.name', 'DerasaX')}</span>
      </div>
      <ul className="app-sidebar__list">
        {items.map((item) => {
          const Icon = item.icon
          return (
            <li key={item.key}>
              <NavLink
                to={item.to}
                end={item.to === '/app'}
                className={({ isActive }) => `app-sidebar__link${isActive ? ' is-active' : ''}`}
                onClick={onNavigate}
              >
                <Icon size={18} aria-hidden="true" />
                <span>{t(item.labelKey)}</span>
              </NavLink>
            </li>
          )
        })}
      </ul>
    </nav>
  )
}
