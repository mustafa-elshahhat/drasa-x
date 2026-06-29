import { describe, it, expect } from 'vitest'
import { navItemsForRole } from './navConfig'
import { ROLES } from '../features/auth/roles'

describe('role-aware navigation filtering', () => {
  it('shows a student only student + shared destinations', () => {
    const keys = navItemsForRole(ROLES.STUDENT).map((i) => i.key)
    expect(keys).toContain('student')
    expect(keys).toContain('profile')
    expect(keys).toContain('security')
    expect(keys).not.toContain('teacher')
    expect(keys).not.toContain('system')
    expect(keys).not.toContain('school')
  })

  it('shows a teacher the Phase 9 teacher portal destinations, not admin areas', () => {
    const keys = navItemsForRole(ROLES.TEACHER).map((i) => i.key)
    expect(keys).toContain('teacher')
    expect(keys).toContain('teacher-classes')
    expect(keys).toContain('teacher-quizzes')
    expect(keys).not.toContain('system')
    expect(keys).not.toContain('parent')
    expect(keys).not.toContain('student')
  })

  it('shows the system admin the platform area but not student learning', () => {
    const keys = navItemsForRole(ROLES.SYSTEM_ADMIN).map((i) => i.key)
    expect(keys).toContain('system')
    expect(keys).not.toContain('student')
  })

  it('never leaks another role’s destinations', () => {
    for (const role of Object.values(ROLES)) {
      const items = navItemsForRole(role)
      for (const item of items) {
        if (item.roles !== 'all') expect(item.roles).toContain(role)
      }
    }
  })
})
