import { describe, it, expect } from 'vitest'
import { directionForLanguage, RTL_LANGUAGES } from './direction'

describe('localization direction handling', () => {
  it('maps Arabic to RTL', () => {
    expect(directionForLanguage('ar')).toBe('rtl')
    expect(directionForLanguage('ar-EG')).toBe('rtl')
  })

  it('maps English (and unknowns) to LTR', () => {
    expect(directionForLanguage('en')).toBe('ltr')
    expect(directionForLanguage('en-US')).toBe('ltr')
    expect(directionForLanguage('fr')).toBe('ltr')
    expect(directionForLanguage(undefined)).toBe('ltr')
  })

  it('treats every configured RTL language as RTL', () => {
    for (const lng of RTL_LANGUAGES) expect(directionForLanguage(lng)).toBe('rtl')
  })
})
