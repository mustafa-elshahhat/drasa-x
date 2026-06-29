import { describe, it, expect } from 'vitest'
import { safeReturnPath, isSafeReturnPath } from './returnUrl'

describe('safeReturnPath (open-redirect prevention)', () => {
  it('accepts in-app relative paths', () => {
    expect(safeReturnPath('/app/teacher')).toBe('/app/teacher')
    expect(safeReturnPath('/app/teacher/assignments?tab=1')).toBe('/app/teacher/assignments?tab=1')
  })

  it('rejects absolute http(s) URLs', () => {
    expect(safeReturnPath('https://evil.example')).toBe('/app')
    expect(safeReturnPath('http://evil.example/app')).toBe('/app')
  })

  it('rejects javascript: and data: schemes', () => {
    expect(safeReturnPath('javascript:alert(1)')).toBe('/app')
    expect(safeReturnPath('data:text/html,evil')).toBe('/app')
  })

  it('rejects protocol-relative and backslash tricks', () => {
    expect(safeReturnPath('//evil.example')).toBe('/app')
    expect(safeReturnPath('/\\evil.example')).toBe('/app')
    expect(safeReturnPath('\\\\evil.example')).toBe('/app')
  })

  it('rejects non-rooted and whitespace-laced paths', () => {
    expect(safeReturnPath('app/teacher')).toBe('/app')
    expect(safeReturnPath('/app\tteacher')).toBe('/app')
    expect(safeReturnPath('/app\nteacher')).toBe('/app')
  })

  it('falls back for empty/invalid input', () => {
    expect(safeReturnPath('')).toBe('/app')
    expect(safeReturnPath(null)).toBe('/app')
    expect(safeReturnPath(undefined, '/home')).toBe('/home')
  })

  it('isSafeReturnPath agrees with safeReturnPath', () => {
    expect(isSafeReturnPath('/app/x')).toBe(true)
    expect(isSafeReturnPath('//evil')).toBe(false)
    expect(isSafeReturnPath('https://evil')).toBe(false)
  })
})
