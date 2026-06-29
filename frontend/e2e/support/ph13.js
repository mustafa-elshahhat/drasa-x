// =============================================================================
// Phase 13 live-acceptance shared support (Messaging, Notifications, Real-Time).
// Re-uses the Phase 8 base helpers (real browser → vite preview → DerasaX-backend
// → PostgreSQL). Dedicated PH13-TENANT actors so announcement fan-out is bounded.
// No network mocking; credentials from env.
// =============================================================================
import { request as pwRequest } from '@playwright/test'
export {
  BACKEND, PASSWORD, RESET_KEY, HAS_CREDS, CODES, FIX,
  backendUp, resetE2E, apiLogin, login, nav, setLang, attachGuards, captureRequest,
} from './ph8.js'
import { BACKEND, PASSWORD } from './ph8.js'

// Dedicated Phase 13 seed login codes (DataSeederService Phase 13 fixtures).
export const CODES13 = {
  admin: 'PH13-ADMIN',
  teacher: 'PH13-TEACHER',
  studentA: 'PH13-STUDENT-A',
  studentB: 'PH13-STUDENT-B',
  parent: 'PH13-PARENT',
}

// Deterministic Phase 13 notification fixtures (owned by PH13-STUDENT-A).
export const FIX13 = { notif1: 'PH13-NOTIF-1', notif2: 'PH13-NOTIF-2' }

// Login over the real account contract and return the full model (id + token).
export async function apiLoginFull(code, password = PASSWORD) {
  const ctx = await pwRequest.newContext()
  try {
    const res = await ctx.post(`${BACKEND}/api/v1/account/login`, {
      headers: { 'Content-Type': 'application/json' },
      data: { UserID: code, Password: password },
      timeout: 15000,
    })
    if (!res.ok()) throw new Error(`apiLoginFull ${code} failed: ${res.status()}`)
    const model = await res.json()
    if (!model?.token || !model?.id) throw new Error(`apiLoginFull ${code}: missing id/token`)
    return { id: model.id, token: model.token }
  } finally {
    await ctx.dispose()
  }
}

// Create + publish a tenant announcement via the real SchoolAdmin contract.
// targetAudience: 1=Students, 2=Parents, 4=Teachers, 7=All ([Flags]).
export async function publishAnnouncement(adminToken, { title, body, targetAudience = 1 }) {
  const ctx = await pwRequest.newContext()
  try {
    const create = await ctx.post(`${BACKEND}/api/v1/announcements`, {
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${adminToken}` },
      data: { title, body, targetAudience },
      timeout: 15000,
    })
    if (!create.ok()) throw new Error(`announcement create failed: ${create.status()} ${await create.text()}`)
    const id = (await create.json())?.data?.id
    const publish = await ctx.post(`${BACKEND}/api/v1/announcements/${id}/publish?publish=true`, {
      headers: { Authorization: `Bearer ${adminToken}` },
      timeout: 15000,
    })
    if (!publish.ok()) throw new Error(`announcement publish failed: ${publish.status()}`)
    return id
  } finally {
    await ctx.dispose()
  }
}

// Start a conversation (+ first message) via the real teacher contract; returns its id.
export async function startConversation(teacherToken, { participantUserId, subject, firstMessage }) {
  const ctx = await pwRequest.newContext()
  try {
    const res = await ctx.post(`${BACKEND}/api/v1/conversations`, {
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${teacherToken}` },
      data: { participantUserId, subject, firstMessage },
      timeout: 15000,
    })
    if (!res.ok()) throw new Error(`startConversation failed: ${res.status()} ${await res.text()}`)
    return (await res.json())?.data?.id
  } finally {
    await ctx.dispose()
  }
}

// Read the authenticated user's unread notification count over the real contract.
export async function unreadCount(token) {
  const ctx = await pwRequest.newContext()
  try {
    const res = await ctx.get(`${BACKEND}/api/v1/notifications/unread-count`, {
      headers: { Authorization: `Bearer ${token}` },
      timeout: 8000,
    })
    if (!res.ok()) throw new Error(`unreadCount failed: ${res.status()}`)
    return Number((await res.json())?.unreadCount ?? 0)
  } finally {
    await ctx.dispose()
  }
}
