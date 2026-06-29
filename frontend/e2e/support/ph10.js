// =============================================================================
// Phase 10 Parent Portal live-acceptance support. Real browser → vite preview →
// DerasaX-backend → PostgreSQL. No network mocking. Extends the proven Phase 8
// helpers (login/nav/guards/reset). The Parent Portal reads do NOT call the AI
// service; the browser never calls AI directly regardless.
// =============================================================================
import { request as pwRequest } from '@playwright/test'
import { BACKEND, PASSWORD } from './ph8.js'

export { BACKEND, PASSWORD, RESET_KEY, HAS_CREDS, backendUp, resetE2E, apiLogin, login, nav, setLang, attachGuards, axeSeriousOrCritical } from './ph8.js'

// Deterministic Phase 10 actors (DataSeederService Phase 3/8/10 fixtures).
export const CODES10 = {
  parentLinked: 'PH10-PARENT-T1', // linked to STU-T1 (child with real academic data)
  parentNoChild: 'PH10-PARENT-NOCHILD-T1', // same-tenant parent, no links (empty state)
  studentT1: 'STU-T1', // the linked child
  otherT1: 'PH8-OTHER-T1', // same-tenant, NOT linked to PH10-PARENT-T1 (403)
  studentT2: 'STU-T2', // cross-tenant child (404)
  teacher: 'TEACH-T1',
  schoolAdmin: 'ADMIN-T1',
}

export const CHILD_NAME = 'Tenant1 Student' // STU-T1 full name (seeded)

// Resolve a user's real id (GUID) the way a client does: from the login identity.
// Parent routes take the student's actual id, not the login code.
export async function apiLoginId(code, password = PASSWORD) {
  const ctx = await pwRequest.newContext()
  try {
    const res = await ctx.post(`${BACKEND}/api/v1/account/login`, {
      headers: { 'Content-Type': 'application/json' },
      data: { UserID: code, Password: password },
      timeout: 15000,
    })
    if (!res.ok()) throw new Error(`apiLoginId ${code} failed: ${res.status()}`)
    const model = await res.json()
    const id = model?.id || model?.Id
    if (!id) throw new Error(`apiLoginId ${code}: no id in response`)
    return id
  } finally {
    await ctx.dispose()
  }
}
