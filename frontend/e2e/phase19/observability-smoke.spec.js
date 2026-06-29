// =============================================================================
// Phase 19 — OBSERVABILITY smoke (live).
//
// Browser-origin HTTP (Playwright request) -> DerasaX-backend, exactly as an
// operator/monitor would probe it. Proves the Phase 19 operational surface:
//   * GET /health returns an aggregate document with service identity + version
//     + per-dependency checks (postgres / storage / ai / background-jobs).
//   * /health/live + /health/ready are reachable and echo a correlation id.
//   * An inbound X-Correlation-Id is propagated back verbatim.
// No mocking; honest skip when the backend is not running.
// =============================================================================
import { test, expect, request as pwRequest } from '@playwright/test'

const BACKEND = process.env.VITE_DOTNET_URL || 'http://localhost:5155'

async function backendUp() {
  try {
    const ctx = await pwRequest.newContext()
    const res = await ctx.get(`${BACKEND}/health/live`, { timeout: 3000 })
    await ctx.dispose()
    return res.ok()
  } catch {
    return false
  }
}

test.describe('Phase 19 observability', () => {
  test('aggregate /health reports service identity, version and dependency checks', async () => {
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
    const ctx = await pwRequest.newContext()
    try {
      const res = await ctx.get(`${BACKEND}/health`)
      expect(res.status()).toBe(200)
      const body = await res.json()
      expect(body.service).toBe('derasax-backend')
      expect(typeof body.version).toBe('string')
      expect(body.version.length).toBeGreaterThan(0)
      const names = (body.checks || []).map((c) => c.name)
      for (const expected of ['postgres', 'storage', 'ai', 'background-jobs']) {
        expect(names).toContain(expected)
      }
    } finally {
      await ctx.dispose()
    }
  })

  test('liveness echoes a correlation id on the response', async () => {
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
    const ctx = await pwRequest.newContext()
    try {
      const res = await ctx.get(`${BACKEND}/health/live`)
      expect(res.ok()).toBeTruthy()
      const cid = res.headers()['x-correlation-id']
      expect(cid, 'X-Correlation-Id header must be present').toBeTruthy()
    } finally {
      await ctx.dispose()
    }
  })

  test('inbound X-Correlation-Id is propagated verbatim', async () => {
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
    const ctx = await pwRequest.newContext()
    try {
      const res = await ctx.get(`${BACKEND}/health/live`, {
        headers: { 'X-Correlation-Id': 'e2e-ph19-corr-123' },
      })
      expect(res.ok()).toBeTruthy()
      expect(res.headers()['x-correlation-id']).toBe('e2e-ph19-corr-123')
    } finally {
      await ctx.dispose()
    }
  })

  test('readiness endpoint is reachable', async () => {
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
    const ctx = await pwRequest.newContext()
    try {
      const res = await ctx.get(`${BACKEND}/health/ready`)
      expect(res.status()).toBe(200)
    } finally {
      await ctx.dispose()
    }
  })
})
