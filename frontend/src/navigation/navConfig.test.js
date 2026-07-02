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

  // Audit F-04 / P0-5 / P0-6: homework, competitions and vision are fully built,
  // backend-supported surfaces that were missing from Teacher nav.
  it('surfaces homework, competitions and vision for Teacher', () => {
    const keys = navItemsForRole(ROLES.TEACHER).map((i) => i.key)
    expect(keys).toContain('teacher-homework')
    expect(keys).toContain('teacher-competitions')
    expect(keys).toContain('teacher-vision')
  })

  // SchoolAdmin Teacher-portal removal: SchoolAdmin must see and use ONLY the School
  // Admin portal — no Teacher dashboard, no "My classes"/"My subjects", no Teacher-only
  // nav item at all (a previous pass intentionally shared these; that is no longer the
  // desired product behavior).
  it('never shows any Teacher-portal nav item to SchoolAdmin', () => {
    const keys = navItemsForRole(ROLES.SCHOOL_ADMIN).map((i) => i.key)
    const teacherOnlyKeys = [
      'teacher', 'teacher-classes', 'teacher-subjects', 'teacher-students',
      'teacher-quizzes', 'teacher-generate', 'teacher-homework', 'teacher-competitions',
      'teacher-office-hours', 'teacher-communities', 'teacher-vision', 'teacher-notifications',
    ]
    for (const key of teacherOnlyKeys) expect(keys).not.toContain(key)
    for (const item of navItemsForRole(ROLES.SCHOOL_ADMIN)) {
      expect(item.to.startsWith('/app/teacher')).toBe(false)
      expect(item.to).not.toBe('/app/vision')
    }
  })

  // Audit F-04 / D1: the student vision (computer-vision engagement) surface was
  // registered and backend-ready but had zero nav entry.
  it('surfaces vision for Student', () => {
    const keys = navItemsForRole(ROLES.STUDENT).map((i) => i.key)
    expect(keys).toContain('student-vision')
  })

  // Audit §2.6 / §11 P1 / D12: 7 school-admin routes were registered but
  // reachable only by typing the URL — including the only user-creation page.
  it('surfaces the previously-orphaned school-admin routes (profile, users, terms, grades, storage, communities, competitions)', () => {
    const keys = navItemsForRole(ROLES.SCHOOL_ADMIN).map((i) => i.key)
    for (const key of ['school-profile', 'school-users', 'school-terms', 'school-grades', 'school-storage', 'school-communities', 'school-competitions']) {
      expect(keys).toContain(key)
    }
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
