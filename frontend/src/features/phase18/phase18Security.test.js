// Phase 18 — frontend security-hardening regression pack. Static, deterministic checks that
// the deployed app keeps its security invariants: the static host emits security headers (incl.
// a meaningful CSP), no shipped component renders unsanitized HTML, no auth token is persisted to
// web storage, and no client embeds a direct AI-service / object-storage URL. These complement the
// Phase 17 client-contract tests (backend-relative paths only).
import { describe, it, expect } from 'vitest'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const here = path.dirname(fileURLToPath(import.meta.url))
const root = path.resolve(here, '../../..') // school-ai-frontend
const srcDir = path.join(root, 'src')

function collectSource(dir, acc = []) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name)
    if (entry.isDirectory()) {
      if (entry.name === 'node_modules') continue
      collectSource(full, acc)
    } else if (/\.(js|jsx|ts|tsx)$/.test(entry.name) && !/\.test\.(js|jsx|ts|tsx)$/.test(entry.name)) {
      // Exclude test files (they legitimately contain attack vectors / forbidden strings as data).
      acc.push(full)
    }
  }
  return acc
}

const shippedSource = collectSource(srcDir)

describe('Phase 18 — static host security headers (vercel.json)', () => {
  const vercel = JSON.parse(fs.readFileSync(path.join(root, 'vercel.json'), 'utf8'))
  const headerBlock = (vercel.headers ?? []).find((h) => h.source === '/(.*)')
  const byKey = Object.fromEntries((headerBlock?.headers ?? []).map((h) => [h.key.toLowerCase(), h.value]))

  it('emits the core security headers', () => {
    expect(byKey['x-content-type-options']).toBe('nosniff')
    expect(byKey['x-frame-options']).toBe('DENY')
    expect(byKey['referrer-policy']).toBeTruthy()
    expect(byKey['permissions-policy']).toBeTruthy()
    expect(byKey['cross-origin-opener-policy']).toBe('same-origin')
    expect(byKey['strict-transport-security']).toMatch(/max-age=\d+/)
  })

  it('ships a meaningful Content-Security-Policy', () => {
    const csp = byKey['content-security-policy'] ?? ''
    expect(csp).toContain("default-src 'self'")
    expect(csp).toContain("frame-ancestors 'none'")
    expect(csp).toContain("object-src 'none'")
    expect(csp).toContain("base-uri 'self'")
    // Must NOT silently allow arbitrary inline/eval'd scripts.
    expect(csp).not.toContain("script-src 'unsafe-inline'")
    expect(csp).not.toContain("'unsafe-eval'")
  })
})

describe('Phase 18 — no unsafe HTML rendering in shipped source', () => {
  it('never uses dangerouslySetInnerHTML', () => {
    const offenders = shippedSource.filter((f) => fs.readFileSync(f, 'utf8').includes('dangerouslySetInnerHTML'))
    expect(offenders).toEqual([])
  })
})

describe('Phase 18 — auth tokens are not persisted to web storage', () => {
  it('no shipped source writes a token/jwt/secret to localStorage or sessionStorage', () => {
    const re = /(local|session)Storage\.setItem\(\s*[`'"][^`'"]*(token|jwt|secret|password|bearer)/i
    const offenders = shippedSource.filter((f) => re.test(fs.readFileSync(f, 'utf8')))
    expect(offenders).toEqual([])
  })
})

describe('Phase 18 — no direct AI / object-storage URLs in shipped clients', () => {
  // Match the host only inside a STRING LITERAL on a non-comment line, so documentation
  // comments that name the AI service (to explain the backend-only invariant) are not flagged.
  // The behavioural proof (clients issue backend-relative paths) lives in the Phase 17 contract test.
  const inLiteral = /['"`][^'"`]*(:8000|school-ai-rag|amazonaws\.com|\.r2\.cloudflarestorage|blob\.core\.windows\.net)/i
  it('never embeds an AI-service or object-storage host in code', () => {
    const offenders = shippedSource.filter((f) =>
      fs.readFileSync(f, 'utf8').split('\n').some((line) => {
        const t = line.trim()
        if (t.startsWith('//') || t.startsWith('*') || t.startsWith('/*')) return false
        return inLiteral.test(line)
      }),
    )
    expect(offenders).toEqual([])
  })
})
