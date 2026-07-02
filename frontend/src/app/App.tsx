import { lazy, Suspense } from 'react'
import type { ReactElement } from 'react'
import { Routes, Route } from 'react-router-dom'
import { ROUTES } from './router/routes'
import type { AppRoute } from './router/route.types'
import { AppShell } from '../layouts/AppShell'
import { PublicLayout } from '../layouts/PublicLayout'
import { ProtectedRoute, RoleGuard, PermissionGuard, AnonymousOnly, RequirePasswordChange } from '../features/auth/guards'
import { FullPageLoader } from '../components/ui/FullPageLoader'
import {
  ForbiddenPage,
  SessionExpiredPage,
  SuspendedTenantPage,
  NotFoundPage,
} from '../pages/status/StatusPages'

const ForcedChangePasswordPage = lazy(() => import('../pages/auth/ForcedChangePasswordPage.jsx'))

// Wrap a route element with the guard implied by its metadata.
// `roles` takes precedence over `permission` (they are not combined) — identical
// to the pre-migration behavior.
function guardElement(route: AppRoute, element: ReactElement): ReactElement {
  let node = element
  if (route.roles) node = <RoleGuard roles={route.roles}>{node}</RoleGuard>
  else if (route.permission) node = <PermissionGuard permission={route.permission}>{node}</PermissionGuard>
  return node
}

export default function App() {
  // Marketing pages render inside the light PublicLayout; auth/anonymous pages
  // (e.g. /login) render flat with their own focused layout. The /app shell is
  // unchanged.
  const marketingRoutes = ROUTES.filter((r) => r.layout === 'public')
  const flatPublicRoutes = ROUTES.filter((r) => !r.requiresAuth && r.layout !== 'public')
  const appRoutes = ROUTES.filter((r) => r.requiresAuth && r.path !== '/app')
  const appIndex = ROUTES.find((r) => r.path === '/app')
  const AppIndexComponent = appIndex?.Component

  return (
    <Suspense fallback={<FullPageLoader label="Loading…" />}>
      <Routes>
        {/* Auth / anonymous-only routes (focused layout, no marketing chrome) */}
        {flatPublicRoutes.map((r) => {
          const C = r.Component!
          const el = <C {...(r.props || {})} />
          return (
            <Route
              key={r.path}
              path={r.path}
              element={r.anonymousOnly ? <AnonymousOnly>{el}</AnonymousOnly> : el}
            />
          )
        })}

        {/* Public marketing routes wrapped in the light PublicLayout */}
        <Route element={<PublicLayout />}>
          {marketingRoutes.map((r) => {
            const C = r.Component!
            return <Route key={r.path} path={r.path} element={<C {...(r.props || {})} />} />
          })}
        </Route>

        {/* Authenticated application shell */}
        <Route
          path="/app"
          element={
            <ProtectedRoute>
              <AppShell />
            </ProtectedRoute>
          }
        >
          {AppIndexComponent && <Route index element={<AppIndexComponent />} />}
          {appRoutes.map((r) => {
            const C = r.Component!
            const relative = r.path.replace(/^\/app\//, '')
            return (
              <Route key={r.path} path={relative} element={guardElement(r, <C {...(r.props || {})} />)} />
            )
          })}
        </Route>

        {/* Forced first-login password change — hardcoded (not in the ROUTES registry) so it
            renders standalone, outside the AppShell nav, and doesn't affect routeArchitecture's
            registry invariants (EXPECTED_ROUTE_COUNT). Guarded separately from ProtectedRoute:
            only reachable while mustChangePassword is true. */}
        <Route
          path="/change-password"
          element={
            <RequirePasswordChange>
              <ForcedChangePasswordPage />
            </RequirePasswordChange>
          }
        />

        {/* Status + fallback routes */}
        <Route path="/forbidden" element={<ForbiddenPage />} />
        <Route path="/session-expired" element={<SessionExpiredPage />} />
        <Route path="/suspended" element={<SuspendedTenantPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </Suspense>
  )
}
