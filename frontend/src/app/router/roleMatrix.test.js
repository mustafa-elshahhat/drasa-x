import { describe, it, expect } from 'vitest'
import { ROUTES } from './routes'
import { ROLES, ALL_ROLES, roleHasPermission } from '../../features/auth/roles'

// Testing gap T-06 (audit §10 item 6): a direct-URL role matrix over the real
// route registry. `guardElement` (app/App.tsx) applies `roles` (if present,
// takes precedence) else `permission` (if present) else no extra guard beyond
// authentication — this test evaluates that SAME precedence for every
// route x role combination using the real `roleHasPermission` used at runtime,
// so an accidental role-list change on any route is caught here directly
// rather than only being discoverable by manually typing a URL.

function isAllowed(route, role) {
  if (route.roles) return route.roles.includes(role)
  if (route.permission) return roleHasPermission(role, route.permission)
  return true // authenticated-only, no extra restriction
}

// Prefixes owned exclusively by one role (excludes the shared
// /app/messages|notifications|profile|security surfaces, which intentionally
// allow more than one role). /app/teacher is asserted separately below since it
// shares its owning role, Teacher, with the also-Teacher-only /app/vision prefix.
const EXCLUSIVE_PREFIXES = [
  ['/app/student', ROLES.STUDENT],
  ['/app/parent', ROLES.PARENT],
  ['/app/school', ROLES.SCHOOL_ADMIN],
  ['/app/system', ROLES.SYSTEM_ADMIN],
  // SchoolAdmin Teacher-portal removal: staff computer-vision attendance is a
  // Teacher-portal-only surface with no school-admin equivalent (D1 previously
  // shared it with SchoolAdmin; that is no longer the desired product behavior).
  ['/app/vision', ROLES.TEACHER],
]

describe('direct-URL role matrix (T-06)', () => {
  it('every route x role combination resolves without throwing', () => {
    for (const route of ROUTES) {
      for (const role of ALL_ROLES) {
        expect(() => isAllowed(route, role)).not.toThrow()
      }
    }
  })

  for (const [prefix, owner] of EXCLUSIVE_PREFIXES) {
    it(`only ${owner} may access any route under ${prefix}`, () => {
      const scoped = ROUTES.filter((r) => r.path === prefix || r.path.startsWith(prefix + '/'))
      expect(scoped.length).toBeGreaterThan(0)
      for (const route of scoped) {
        for (const role of ALL_ROLES) {
          const expected = role === owner
          expect(isAllowed(route, role), `${route.path} x ${role} should be ${expected}`).toBe(expected)
        }
      }
    })
  }

  // SchoolAdmin Teacher-portal removal: /app/teacher is now Teacher-exclusive.
  // A previous pass deliberately let SchoolAdmin act on teacher surfaces too
  // (D8/P1-5); that is no longer the desired product behavior — SchoolAdmin must
  // not see, land on, or directly access any /app/teacher/* route.
  it('only Teacher may access any route under /app/teacher', () => {
    const scoped = ROUTES.filter((r) => r.path === '/app/teacher' || r.path.startsWith('/app/teacher/'))
    expect(scoped.length).toBeGreaterThan(0)
    for (const route of scoped) {
      for (const role of ALL_ROLES) {
        const expected = role === ROLES.TEACHER
        expect(isAllowed(route, role), `${route.path} x ${role} should be ${expected}`).toBe(expected)
      }
    }
  })

  // D5/P1-9: /app/messages(+thread) is every tenant role except SystemAdmin.
  it('/app/messages routes exclude SystemAdmin only', () => {
    const scoped = ROUTES.filter((r) => r.path === '/app/messages' || r.path === '/app/messages/:conversationId')
    expect(scoped).toHaveLength(2)
    for (const route of scoped) {
      for (const role of ALL_ROLES) {
        const expected = role !== ROLES.SYSTEM_ADMIN
        expect(isAllowed(route, role), `${route.path} x ${role} should be ${expected}`).toBe(expected)
      }
    }
  })

  // /app/notifications(+preferences) and /app/vision/student|parent scoped
  // surfaces: no `roles` array at all means every authenticated role passes
  // the route guard (further scoping, if any, happens at the backend/data
  // level) — pin that this is still true for the shared notification routes.
  it('/app/notifications routes have no role restriction (every authenticated role passes the guard)', () => {
    const scoped = ROUTES.filter((r) => r.path === '/app/notifications' || r.path === '/app/notifications/preferences')
    expect(scoped).toHaveLength(2)
    for (const route of scoped) {
      for (const role of ALL_ROLES) {
        expect(isAllowed(route, role)).toBe(true)
      }
    }
  })
})
