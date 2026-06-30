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
  roles?: string[]
  /** Required frontend permission (used when `roles` is absent). */
  permission?: string
  /** Lazily-loaded page component. */
  Component?: RouteComponent
  /** Static props passed to the component (e.g. the temporary `{ view }`). */
  props?: Record<string, unknown>
}
