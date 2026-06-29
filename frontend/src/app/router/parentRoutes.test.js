import { describe, expect, it } from 'vitest'
import { ROUTES } from './routes'
import { navItemsForRole } from '../../navigation/navConfig'
import { ROLES } from '../../features/auth/roles'

const parentRoutes = ROUTES.filter((r) => typeof r.path === 'string' && r.path.startsWith('/app/parent'))

describe('Phase 10 parent route guards', () => {
  it('exposes a real parent portal route surface (not a placeholder)', () => {
    expect(parentRoutes.length).toBeGreaterThanOrEqual(8)
  })

  it('restricts every parent route to the Parent role only', () => {
    for (const route of parentRoutes) {
      expect(route.requiresAuth).toBe(true)
      expect(route.roles).toBeDefined()
      expect(route.roles).toContain(ROLES.PARENT)
      // Other roles must never be allowed on parent routes.
      expect(route.roles).not.toContain(ROLES.STUDENT)
      expect(route.roles).not.toContain(ROLES.TEACHER)
      expect(route.roles).not.toContain(ROLES.SCHOOL_ADMIN)
      expect(route.roles).not.toContain(ROLES.SYSTEM_ADMIN)
    }
  })

  it('routes the parent dashboard, children, child detail, attendance, and documents', () => {
    const paths = parentRoutes.map((r) => r.path)
    expect(paths).toContain('/app/parent')
    expect(paths).toContain('/app/parent/children')
    expect(paths).toContain('/app/parent/children/:childId')
    expect(paths).toContain('/app/parent/children/:childId/attendance')
    expect(paths).toContain('/app/parent/documents')
  })

  it('shows parent nav items to parents but not to students or teachers', () => {
    const parentNav = navItemsForRole(ROLES.PARENT).filter((i) => i.to.startsWith('/app/parent'))
    expect(parentNav.length).toBeGreaterThanOrEqual(4)
    expect(navItemsForRole(ROLES.STUDENT).some((i) => i.to.startsWith('/app/parent'))).toBe(false)
    expect(navItemsForRole(ROLES.TEACHER).some((i) => i.to.startsWith('/app/parent'))).toBe(false)
  })
})
