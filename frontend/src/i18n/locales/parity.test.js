import { describe, it, expect } from 'vitest'
import en from './en'
import ar from './ar'

// Guards the migration's i18n rule: every key added to one locale must be added
// to the other at the same path. A missing translation fails here rather than
// silently rendering a key/English fallback in the Arabic UI.
function keyPaths(obj, prefix = '') {
  const out = []
  for (const [k, v] of Object.entries(obj)) {
    const path = prefix ? `${prefix}.${k}` : k
    if (v && typeof v === 'object' && !Array.isArray(v)) out.push(...keyPaths(v, path))
    else out.push(path)
  }
  return out
}

describe('i18n locale parity', () => {
  it('en and ar expose an identical set of translation keys', () => {
    const enKeys = keyPaths(en.translation).sort()
    const arKeys = keyPaths(ar.translation).sort()
    const missingInAr = enKeys.filter((k) => !arKeys.includes(k))
    const missingInEn = arKeys.filter((k) => !enKeys.includes(k))
    expect({ missingInAr, missingInEn }).toEqual({ missingInAr: [], missingInEn: [] })
  })
})
