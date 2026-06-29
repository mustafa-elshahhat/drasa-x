import { describe, expect, it } from 'vitest'
import { ROUTES } from './routes'
import { navItemsForRole } from '../../navigation/navConfig'
import { ROLES } from '../../features/auth/roles'

const teacherRoutes = ROUTES.filter((r) => typeof r.path === 'string' && r.path.startsWith('/app/teacher'))

describe('Phase 9 teacher route guards', () => {
  it('exposes a real teacher portal route surface', () => {
    expect(teacherRoutes.length).toBeGreaterThanOrEqual(10)
  })

  it('restricts every teacher route to Teacher and SchoolAdmin only', () => {
    for (const route of teacherRoutes) {
      expect(route.requiresAuth).toBe(true)
      expect(route.roles).toBeDefined()
      expect(route.roles).toContain(ROLES.TEACHER)
      // Student / Parent / SystemAdmin must never be allowed on teacher routes.
      expect(route.roles).not.toContain(ROLES.STUDENT)
      expect(route.roles).not.toContain(ROLES.PARENT)
      expect(route.roles).not.toContain(ROLES.SYSTEM_ADMIN)
    }
  })

  it('routes the teacher dashboard and the AI draft generator', () => {
    const paths = teacherRoutes.map((r) => r.path)
    expect(paths).toContain('/app/teacher')
    expect(paths).toContain('/app/teacher/quiz-generate')
    expect(paths).toContain('/app/teacher/submissions/:attemptId')
  })

  it('shows teacher nav items to teachers but not to students', () => {
    const teacherNav = navItemsForRole(ROLES.TEACHER).filter((i) => i.to.startsWith('/app/teacher'))
    expect(teacherNav.length).toBeGreaterThanOrEqual(5)
    const studentNav = navItemsForRole(ROLES.STUDENT)
    expect(studentNav.some((i) => i.to.startsWith('/app/teacher'))).toBe(false)
  })
})
