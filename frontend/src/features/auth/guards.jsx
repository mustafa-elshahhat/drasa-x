// =============================================================================
// Route guards (Phase 7 §5). These control NAVIGATION and UX only. Every
// protected backend endpoint independently enforces authorization from the
// signed token — hidden UI is not security.
//
// While the session is resolving, guards render a full-page loader so protected
// content is never shown before auth is known (no flash of protected content).
// =============================================================================
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from './AuthContext'
import { roleHasPermission } from './roles'
import { FullPageLoader } from '../../shared/feedback'

/** Requires an authenticated session. Anonymous -> /login (remembers origin). */
export function ProtectedRoute({ children }) {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'loading') return <FullPageLoader label="Restoring your session…" />
  if (status === 'suspended') return <Navigate to="/suspended" replace />
  if (status !== 'authenticated')
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />

  return children
}

/** Only for anonymous users (login/landing). Authenticated -> their home. */
export function AnonymousOnly({ children, redirectTo = '/app' }) {
  const { status } = useAuth()
  if (status === 'loading') return <FullPageLoader label="Loading…" />
  if (status === 'authenticated') return <Navigate to={redirectTo} replace />
  return children
}

/** Restrict to one or more roles. Wrong role -> /forbidden. */
export function RoleGuard({ roles, children }) {
  const { status, role } = useAuth()
  const location = useLocation()

  if (status === 'loading') return <FullPageLoader label="Restoring your session…" />
  if (status === 'suspended') return <Navigate to="/suspended" replace />
  if (status !== 'authenticated')
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
  if (roles && roles.length > 0 && !roles.includes(role))
    return <Navigate to="/forbidden" replace />

  return children
}

/** Restrict by a frontend permission flag. Missing permission -> /forbidden. */
export function PermissionGuard({ permission, children }) {
  const { status, role } = useAuth()
  const location = useLocation()

  if (status === 'loading') return <FullPageLoader label="Restoring your session…" />
  if (status === 'suspended') return <Navigate to="/suspended" replace />
  if (status !== 'authenticated')
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
  if (permission && !roleHasPermission(role, permission))
    return <Navigate to="/forbidden" replace />

  return children
}

/** Blocks the app when the tenant is suspended. */
export function TenantStatusGuard({ children }) {
  const { status } = useAuth()
  if (status === 'suspended') return <Navigate to="/suspended" replace />
  return children
}
