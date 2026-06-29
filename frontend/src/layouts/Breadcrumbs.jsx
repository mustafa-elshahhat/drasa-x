import { Link, useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ChevronRight } from 'lucide-react'
import { findRouteByPath } from '../app/router/routes'

// Breadcrumb trail derived from the current path + route metadata (Phase 7 §6).
export function Breadcrumbs() {
  const location = useLocation()
  const { t } = useTranslation()
  const segments = location.pathname.split('/').filter(Boolean)

  // Build cumulative paths: /app, /app/teacher, ...
  const crumbs = segments.map((_, i) => {
    const path = '/' + segments.slice(0, i + 1).join('/')
    const route = findRouteByPath(path)
    const labelKey = route?.titleKey
    return { path, label: labelKey ? t(labelKey) : segments[i] }
  })

  if (crumbs.length <= 1) return null

  return (
    <nav className="ui-breadcrumbs" aria-label="Breadcrumb">
      <ol>
        {crumbs.map((c, i) => (
          <li key={c.path}>
            {i < crumbs.length - 1 ? (
              <>
                <Link to={c.path}>{c.label}</Link>
                <ChevronRight size={14} aria-hidden="true" className="ui-breadcrumbs__sep" />
              </>
            ) : (
              <span aria-current="page">{c.label}</span>
            )}
          </li>
        ))}
      </ol>
    </nav>
  )
}
