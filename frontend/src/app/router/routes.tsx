// =============================================================================
// Central route registry (Phase 7 §6) — now typed and split per area.
//
// Every route declares metadata (path, title, auth requirement, allowed
// roles/permission, nav visibility, a lazily-loaded component) so routing,
// breadcrumbs, navigation, and guards all read from ONE source of truth. The
// per-area files keep each surface small; this module only concatenates them
// into the `ROUTES` array consumers already import. No path/guard/prop changed.
// =============================================================================
import { matchPath } from 'react-router-dom'
import type { AppRoute } from './route.types'
import { publicRoutes } from './routes.public'
import { authRoutes } from './routes.auth'
import { sharedRoutes } from './routes.shared'
import { visionRoutes } from './routes.vision'
import { studentRoutes } from './routes.student'
import { teacherRoutes } from './routes.teacher'
import { parentRoutes } from './routes.parent'
import { schoolRoutes } from './routes.school'
import { systemRoutes } from './routes.system'

export type { AppRoute } from './route.types'

export const ROUTES: AppRoute[] = [
  ...publicRoutes,
  ...authRoutes,
  ...sharedRoutes,
  ...visionRoutes,
  ...studentRoutes,
  ...teacherRoutes,
  ...parentRoutes,
  ...schoolRoutes,
  ...systemRoutes,
]

/** Look up route metadata by exact path (used by breadcrumbs/title). */
export function findRouteByPath(path: string): AppRoute | null {
  return ROUTES.find((r) => r.path === path) || null
}

/**
 * True when `path` (a concrete URL, e.g. `/app/teacher/units/unit-1`) resolves to
 * SOME registered route pattern (including `:param` routes) — not just an exact
 * literal match. Used by breadcrumbs to decide whether an intermediate crumb is
 * safe to render as a `Link` (F-05: several cumulative breadcrumb paths, like the
 * bare `/app/teacher/units` with no id, are not registered on their own and would
 * otherwise 404).
 */
export function matchesRegisteredRoute(path: string): boolean {
  return ROUTES.some((r) => matchPath({ path: r.path, end: true }, path) !== null)
}
