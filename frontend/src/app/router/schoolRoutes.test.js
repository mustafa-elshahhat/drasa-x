import { describe, expect, it } from 'vitest'
import { ROUTES } from './routes'
import { navItemsForRole } from '../../navigation/navConfig'
import { ROLES } from '../../features/auth/roles'

const schoolRoutes = ROUTES.filter((r) => typeof r.path === 'string' && r.path.startsWith('/app/school'))

describe('Phase 11 school-admin route guards', () => {
  it('exposes a real school-admin portal route surface (not a placeholder)', () => {
    expect(schoolRoutes.length).toBeGreaterThanOrEqual(20)
  })

  it('restricts every school route to the SchoolAdmin role only', () => {
    for (const route of schoolRoutes) {
      expect(route.requiresAuth).toBe(true)
      expect(route.roles).toBeDefined()
      expect(route.roles).toContain(ROLES.SCHOOL_ADMIN)
      // No other role may reach a school-admin route.
      expect(route.roles).not.toContain(ROLES.STUDENT)
      expect(route.roles).not.toContain(ROLES.TEACHER)
      expect(route.roles).not.toContain(ROLES.PARENT)
      expect(route.roles).not.toContain(ROLES.SYSTEM_ADMIN)
    }
  })

  it('routes the core school-admin surfaces required by Phase 11', () => {
    const paths = schoolRoutes.map((r) => r.path)
    for (const p of [
      '/app/school',
      '/app/school/profile',
      '/app/school/academic-years',
      '/app/school/terms',
      '/app/school/grades',
      '/app/school/classes',
      '/app/school/subjects',
      '/app/school/students',
      '/app/school/teachers',
      '/app/school/parents',
      '/app/school/relationships',
      '/app/school/teacher-assignments',
      '/app/school/curriculum',
      '/app/school/announcements',
      '/app/school/document-requests',
      '/app/school/communities',
      '/app/school/competitions',
      '/app/school/reports',
      '/app/school/ai-usage',
      '/app/school/storage',
      '/app/school/subscription',
      '/app/school/support',
      '/app/school/audit-log',
      '/app/school/settings',
    ]) {
      expect(paths).toContain(p)
    }
  })

  it('shows school nav items to school admins but not to other roles', () => {
    const adminNav = navItemsForRole(ROLES.SCHOOL_ADMIN).filter((i) => i.to.startsWith('/app/school'))
    expect(adminNav.length).toBeGreaterThanOrEqual(8)
    for (const role of [ROLES.STUDENT, ROLES.TEACHER, ROLES.PARENT]) {
      expect(navItemsForRole(role).some((i) => i.to.startsWith('/app/school'))).toBe(false)
    }
  })

  // The generic School Admin "Users" page (role dropdown + create form) was removed —
  // account creation now happens from the Students/Teachers/Parents pages only.
  it('no longer routes or navigates to the generic /app/school/users page', () => {
    const paths = schoolRoutes.map((r) => r.path)
    expect(paths).not.toContain('/app/school/users')
    expect(paths).not.toContain('/app/school/users/:userId')
    expect(navItemsForRole(ROLES.SCHOOL_ADMIN).some((i) => i.to === '/app/school/users')).toBe(false)
  })

  it('lets the Students/Teachers/Parents pages create accounts for their fixed role', () => {
    const byPath = Object.fromEntries(schoolRoutes.map((r) => [r.path, r]))
    expect(byPath['/app/school/students'].props).toMatchObject({ role: 'Student', canCreate: true })
    expect(byPath['/app/school/teachers'].props).toMatchObject({ role: 'Teacher', canCreate: true })
    expect(byPath['/app/school/parents'].props).toMatchObject({ role: 'Parent', canCreate: true })
  })
})
