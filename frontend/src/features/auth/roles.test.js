import { describe, it, expect } from 'vitest'
import {
  ROLES,
  ALL_ROLES,
  PERMISSIONS,
  permissionsForRole,
  roleHasPermission,
  isKnownRole,
  homeRouteForRole,
} from './roles'

describe('roles & permissions', () => {
  it('exposes the five backend roles', () => {
    expect(ALL_ROLES).toEqual(['Student', 'Teacher', 'Parent', 'SchoolAdmin', 'SystemAdmin'])
  })

  it('grants every role the ability to manage its own account', () => {
    for (const role of ALL_ROLES) {
      expect(roleHasPermission(role, PERMISSIONS.MANAGE_OWN_ACCOUNT)).toBe(true)
    }
  })

  it('scopes class analytics to teacher/school-admin only', () => {
    expect(roleHasPermission(ROLES.TEACHER, PERMISSIONS.VIEW_CLASS_ANALYTICS)).toBe(true)
    expect(roleHasPermission(ROLES.SCHOOL_ADMIN, PERMISSIONS.VIEW_CLASS_ANALYTICS)).toBe(true)
    expect(roleHasPermission(ROLES.STUDENT, PERMISSIONS.VIEW_CLASS_ANALYTICS)).toBe(false)
    expect(roleHasPermission(ROLES.PARENT, PERMISSIONS.VIEW_CLASS_ANALYTICS)).toBe(false)
  })

  it('reserves platform management for the system admin', () => {
    expect(roleHasPermission(ROLES.SYSTEM_ADMIN, PERMISSIONS.MANAGE_PLATFORM)).toBe(true)
    expect(roleHasPermission(ROLES.SCHOOL_ADMIN, PERMISSIONS.MANAGE_PLATFORM)).toBe(false)
  })

  it('returns no permissions for unknown roles', () => {
    expect(permissionsForRole('Hacker')).toEqual([])
    expect(isKnownRole('Hacker')).toBe(false)
    expect(isKnownRole(ROLES.STUDENT)).toBe(true)
  })

  it('routes each role to its landing area', () => {
    expect(homeRouteForRole(ROLES.STUDENT)).toBe('/app/student')
    expect(homeRouteForRole(ROLES.TEACHER)).toBe('/app/teacher')
    expect(homeRouteForRole(ROLES.PARENT)).toBe('/app/parent')
    expect(homeRouteForRole(ROLES.SCHOOL_ADMIN)).toBe('/app/school')
    expect(homeRouteForRole(ROLES.SYSTEM_ADMIN)).toBe('/app/system')
    expect(homeRouteForRole(null)).toBe('/app')
  })
})
