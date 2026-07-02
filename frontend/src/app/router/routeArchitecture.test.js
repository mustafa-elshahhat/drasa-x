import { describe, it, expect } from 'vitest'
import { readdirSync, readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'
import { ROUTES, findRouteByPath } from './routes'
import { ROLES, PERMISSIONS, ALL_ROLES } from '../../features/auth/roles'

// =============================================================================
// Phase 13.6 — architecture regression guards for the route registry.
//
// These LOCK the post-migration invariants so a future change can't silently
// reintroduce the old PortalPage + props.view dispatcher or drop a guard. They
// strengthen (never weaken) the existing routeInventory.test.js.
// =============================================================================

// The verified current production route count (ROUTES.length at runtime — the
// authoritative count; the `r()` helper hides `path:` from a text grep). If a
// route is intentionally added/removed, update this number in the SAME change —
// that is the point of the guard.
// P1-1/P1-2 (audit-driven fix pass): +5 routes — /app/school/{users,students,
// teachers,parents}/:userId (school-admin user detail) and
// /app/school/classes/:classId (class detail/roster/enrollment).
// Task 3 (audit-driven fix pass, forgot/reset password flow): +2 routes —
// /forgot-password, /reset-password (new anonymous-only auth routes).
// Task 5 (audit-driven fix pass, suggestions moderation): +1 route —
// /app/school/suggestions (school-admin suggestion moderation page).
// Contract-gap appendix (audit-driven fix pass, communities/gamification):
// +2 routes — /app/teacher/communities (new teacher community creation/
// moderation UI) and /app/school/gamification-rules (school-admin rule editor).
// School Admin "Users" page removal: -2 routes — /app/school/users (generic
// role-dropdown listing) and /app/school/users/:userId (superseded by the
// role-scoped /app/school/{students,teachers,parents}/:userId detail routes,
// which already existed). Account creation now happens only from the
// Students/Teachers/Parents pages.
const EXPECTED_ROUTE_COUNT = 139

describe('route architecture (Phase 13 regression guards)', () => {
  it('has the exact verified route count (no silent add/remove)', () => {
    expect(ROUTES.length).toBe(EXPECTED_ROUTE_COUNT)
  })

  it('every route has a string path and a (lazy) Component', () => {
    for (const r of ROUTES) {
      expect(typeof r.path).toBe('string')
      expect(r.path.length).toBeGreaterThan(0)
      expect(r.Component).toBeTruthy()
    }
  })

  it('has no duplicate paths', () => {
    const paths = ROUTES.map((r) => r.path)
    expect(new Set(paths).size).toBe(paths.length)
  })

  it('NO production route carries a `props.view` dispatcher prop', () => {
    for (const r of ROUTES) {
      if (r.props) expect(Object.prototype.hasOwnProperty.call(r.props, 'view')).toBe(false)
    }
  })

  it('every authenticated /app route requires auth', () => {
    for (const r of ROUTES) {
      if (r.path === '/app' || r.path.startsWith('/app/')) {
        expect(r.requiresAuth, `route ${r.path} must requiresAuth`).toBe(true)
      }
    }
  })

  it('no route is both requiresAuth and anonymousOnly', () => {
    for (const r of ROUTES) {
      expect(r.requiresAuth && r.anonymousOnly).toBeFalsy()
    }
  })

  it('public marketing routes are layout="public" and not auth-gated', () => {
    for (const p of ['/', '/request-demo', '/events', '/activities', '/news']) {
      const r = findRouteByPath(p)
      expect(r, `public route ${p} present`).toBeTruthy()
      expect(r.layout).toBe('public')
      expect(r.requiresAuth).toBeFalsy()
    }
  })

  it('role-protected areas preserve their expected role', () => {
    const expectations = [
      ['/app/student', ROLES.STUDENT],
      ['/app/teacher', ROLES.TEACHER],
      ['/app/parent', ROLES.PARENT],
      ['/app/school', ROLES.SCHOOL_ADMIN],
      ['/app/system', ROLES.SYSTEM_ADMIN],
    ]
    for (const [prefix, role] of expectations) {
      const scoped = ROUTES.filter((r) => r.path === prefix || r.path.startsWith(prefix + '/'))
      expect(scoped.length, `${prefix} has routes`).toBeGreaterThan(0)
      for (const r of scoped) {
        if (r.roles) {
          expect(r.roles, `${r.path} keeps ${role}`).toContain(role)
        }
      }
    }
  })

  it('every declared role on a route is a recognized backend role', () => {
    for (const r of ROUTES) {
      if (!r.roles) continue
      for (const role of r.roles) expect(ALL_ROLES).toContain(role)
    }
  })

  it('permission-protected account routes keep MANAGE_OWN_ACCOUNT', () => {
    for (const p of ['/app/profile', '/app/security']) {
      expect(findRouteByPath(p)?.permission).toBe(PERMISSIONS.MANAGE_OWN_ACCOUNT)
    }
  })
})

// Source-level guard: the route source files must not import a *PortalPage
// dispatcher nor declare a `view:` prop (the two removed-dispatcher signatures).
describe('route source files carry no dispatcher remnants', () => {
  const here = dirname(fileURLToPath(import.meta.url))
  const routeFiles = readdirSync(here).filter((f) => /^routes\.[a-z]+\.tsx$/.test(f))
  const stripComments = (s) => s.replace(/\/\*[\s\S]*?\*\//g, '').replace(/(^|[^:])\/\/.*$/gm, '$1')

  it('discovers the split route files', () => {
    expect(routeFiles.length).toBeGreaterThanOrEqual(9)
  })

  for (const f of routeFiles) {
    it(`${f} imports no *PortalPage and declares no view: prop`, () => {
      const code = stripComments(readFileSync(join(here, f), 'utf8'))
      expect(code).not.toMatch(/PortalPage/)
      expect(code).not.toMatch(/\bview\s*:/)
    })
  }
})
