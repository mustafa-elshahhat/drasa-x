import { Link, NavLink } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Shield, LifeBuoy } from 'lucide-react'
import { navItemsForRole } from '../navigation/navConfig'
import { ROLES } from '../features/auth/roles'

// Where the sidebar "Need help?" card sends each role. Admin roles have a support
// inbox; tenant roles use the shared messaging surface.
function helpDestination(role) {
  if (role === ROLES.SYSTEM_ADMIN) return '/app/system/support'
  if (role === ROLES.SCHOOL_ADMIN) return '/app/school/support'
  return '/app/messages'
}

// Sidebar navigation (Phase 7 §6/§7; prototype-parity rework). Shows a role
// "Portal" pill, only the destinations permitted for the current role, and a
// help card pinned to the bottom — matching the prototype side rail. The brand
// wordmark lives in the header. Active state via NavLink's aria-current.
export function Sidebar({ role, onNavigate }) {
  const { t } = useTranslation()
  const items = navItemsForRole(role)

  return (
    <div className="app-sidebar">
      {role && (
        <div className="app-sidebar__role">
          <Shield size={14} aria-hidden="true" />
          <span>
            {t(`roles.${role}`, role)} {t('nav.portal', 'Portal')}
          </span>
        </div>
      )}

      <nav className="app-sidebar__nav" aria-label={t('nav.mainNavigation', 'Main navigation')}>
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

      <div className="app-sidebar__help">
        <p className="app-sidebar__help-title">{t('nav.needHelp', 'Need help?')}</p>
        <p className="app-sidebar__help-body">{t('nav.needHelpBody', 'Reach support or message your school team.')}</p>
        <Link to={helpDestination(role)} className="app-sidebar__help-cta" onClick={onNavigate}>
          <LifeBuoy size={15} aria-hidden="true" />
          <span>{t('nav.helpCta', 'Get help')}</span>
        </Link>
      </div>
    </div>
  )
}
