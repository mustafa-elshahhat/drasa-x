// =============================================================================
// Typed route metadata (Phase 7 §6 registry, now TypeScript).
//
// The shape is identical to the previous JSDoc `AppRoute` typedef — routing,
// breadcrumbs, navigation, and guards all read from ONE source of truth (the
// `ROUTES` array aggregated in `routes.tsx`). Splitting the registry into typed
// per-area files does not change any path, guard, role, permission, or prop.
// =============================================================================

/**
 * A lazily code-split page component — exactly what `React.lazy()` returns.
 * Page props are component-defined (some pages take a temporary `{ view }`),
 * so this stays as permissive as `lazy()` itself rather than fixing a prop shape.
 * (Type-only module query: no runtime import.)
 */
export type RouteComponent = ReturnType<(typeof import('react'))['lazy']>

/** `layout: 'public'` renders the page inside the marketing PublicLayout. */
export type RouteLayout = 'public'

// Role/Permission are the recognized backend role + frontend permission unions
// (single source of truth: features/auth/roles.ts). Importing them here makes the
// route registry's guard metadata strongly typed.
import type { Role, Permission } from '../../features/auth/roles'

export interface AppRoute {
  /** URL path (React Router pattern; may contain `:params`). */
  path: string
  /** i18n key for the page/breadcrumb title. */
  titleKey?: string
  /** Requires an authenticated session (renders inside the /app shell). */
  requiresAuth?: boolean
  /** Only reachable while signed out (e.g. /login). */
  anonymousOnly?: boolean
  /** Marketing layout opt-in. */
  layout?: RouteLayout
  /** Allowed roles (undefined = any authenticated role). */
  roles?: Role[]
  /** Required frontend permission (used when `roles` is absent). */
  permission?: Permission
  /** Lazily-loaded page component. */
  Component?: RouteComponent
  /**
   * Static props passed to the component (e.g. a small page variant like
   * `{ mode: 'progress' }`). NOTE: this is NOT the old `props.view` dispatcher —
   * production routes carry no `view` prop (see routeInventory regression test).
   */
  props?: Record<string, unknown>
}

/** Alias used where "route metadata" reads better than "AppRoute". */
export type RouteMeta = AppRoute
