import { describe, expect, it } from 'vitest'
import { ROUTES } from './routes'
import { navItemsForRole } from '../../navigation/navConfig'
import { ROLES } from '../../features/auth/roles'

const systemRoutes = ROUTES.filter((r) => typeof r.path === 'string' && r.path.startsWith('/app/system'))

describe('Phase 12 system-admin route guards', () => {
  it('exposes a real system-admin portal route surface (not a placeholder)', () => {
    expect(systemRoutes.length).toBeGreaterThanOrEqual(17)
  })

  it('restricts every system route to the SystemAdmin role only', () => {
    for (const route of systemRoutes) {
      expect(route.requiresAuth).toBe(true)
      expect(route.roles).toBeDefined()
      expect(route.roles).toContain(ROLES.SYSTEM_ADMIN)
      // No tenant-scoped role may reach a platform-admin route.
      expect(route.roles).not.toContain(ROLES.STUDENT)
      expect(route.roles).not.toContain(ROLES.TEACHER)
      expect(route.roles).not.toContain(ROLES.PARENT)
      expect(route.roles).not.toContain(ROLES.SCHOOL_ADMIN)
    }
  })

  it('routes the core system-admin surfaces required by Phase 12', () => {
    const paths = systemRoutes.map((r) => r.path)
    for (const p of [
      '/app/system',
      '/app/system/tenants',
      '/app/system/tenants/:tenantId',
      '/app/system/onboarding',
      '/app/system/plans',
      '/app/system/subscriptions',
      '/app/system/usage',
      '/app/system/ai-usage',
      '/app/system/storage',
      '/app/system/support',
      '/app/system/feature-flags',
      '/app/system/announcements',
      '/app/system/health',
      '/app/system/errors',
      '/app/system/audit',
      '/app/system/security',
      '/app/system/backups',
      '/app/system/settings',
    ]) {
      expect(paths).toContain(p)
    }
  })

  it('shows system nav items to system admins but not to other roles', () => {
    const sysNav = navItemsForRole(ROLES.SYSTEM_ADMIN).filter((i) => i.to.startsWith('/app/system'))
    expect(sysNav.length).toBeGreaterThanOrEqual(8)
    for (const role of [ROLES.STUDENT, ROLES.TEACHER, ROLES.PARENT, ROLES.SCHOOL_ADMIN]) {
      expect(navItemsForRole(role).some((i) => i.to.startsWith('/app/system'))).toBe(false)
    }
  })
})
