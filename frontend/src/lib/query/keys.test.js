import { describe, it, expect } from 'vitest'
import { scopedKey, queryKeys } from './keys'

describe('tenant/user-aware query keys', () => {
  it('scopes keys by user id so two users never share cache', () => {
    const a = scopedKey('user-a', ['profile'])
    const b = scopedKey('user-b', ['profile'])
    expect(a).not.toEqual(b)
    expect(a[1]).toBe('user-a')
    expect(b[1]).toBe('user-b')
  })

  it('uses a stable anonymous scope when no user id is present', () => {
    expect(scopedKey(null, ['x'])[1]).toBe('__anon__')
    expect(scopedKey(undefined, ['x'])).toEqual(scopedKey(null, ['x']))
  })

  it('builds consistent profile keys', () => {
    expect(queryKeys.profile('u1')).toEqual(['u', 'u1', 'profile'])
  })

  it('scopes student portal keys by user and feature', () => {
    expect(queryKeys.student.root('student-1')).toEqual(['u', 'student-1', 'student'])
    expect(queryKeys.student.dashboard('student-1')).toEqual(['u', 'student-1', 'student', 'dashboard'])
    expect(queryKeys.student.attendance('student-1')).toEqual(['u', 'student-1', 'student', 'attendance'])
    expect(queryKeys.student.attemptResult('student-1', 'attempt-1')).toEqual([
      'u',
      'student-1',
      'student',
      'attempt',
      'attempt-1',
      'result',
    ])
  })
})
