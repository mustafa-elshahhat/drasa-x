// =============================================================================
// Phase 10 — Parent Portal data access. Uses the SINGLE canonical API client
// (DerasaX-backend only); the browser never calls school-ai-rag. Every read is
// authorized server-side against the parent's ACTIVE, progress-permitted
// parent-student links — the frontend shows only what the backend returns and
// never fabricates progress, attendance, grades, or AI insight data.
//
// Endpoints:
//   * Parent-scoped aggregation (Phase 10 backend contract): /api/v1/parent/...
//   * Deeper per-child reads reuse the relationship-authorized student endpoints
//     (/api/v1/students/{childId}/...), which already permit a linked parent.
//   * Document requests, conversations, notifications, announcements reuse the
//     existing tenant-scoped contracts.
// =============================================================================
import { api } from '../../lib/api/client'
import { toItems, toObject, unwrapEnvelope, normalizeAttendance } from '../student/studentSchemas'
import { downloadToBrowser } from '../files/filesApi'

export const parentApi = {
  // ---- parent-scoped summary + children (Phase 10 backend contract) ----
  async dashboard(signal) {
    return toObject(await api.get('/api/v1/parent/dashboard', { signal }))
  },
  async children(signal) {
    return toItems(await api.get('/api/v1/parent/children', { signal }))
  },
  async child(childId, signal) {
    return toObject(await api.get(`/api/v1/parent/children/${encodeURIComponent(childId)}`, { signal }))
  },
  async childAttendance(childId, signal) {
    return normalizeAttendance(await api.get(`/api/v1/parent/children/${encodeURIComponent(childId)}/attendance`, { signal }))
  },

  // ---- deeper per-child reads (relationship-authorized student endpoints) ----
  async childProgress(childId, signal) {
    const base = `/api/v1/students/${encodeURIComponent(childId)}`
    const [summary, subjects, attempts, insights, recommendations, painPoints] = await Promise.allSettled([
      api.get(`${base}/progress-summary`, { signal }),
      api.get(`${base}/subject-progress`, { signal }),
      api.get(`${base}/attempt-history`, { signal }),
      api.get(`${base}/insights`, { signal }),
      api.get(`${base}/recommendations`, { signal }),
      api.get(`${base}/pain-points`, { signal }),
    ])
    return { summary, subjects, attempts, insights, recommendations, painPoints }
  },

  // ---- read-only gamification for a linked child (relationship-authorized student endpoints) ----
  async childPoints(childId, signal) {
    return toObject(await api.get(`/api/v1/students/${encodeURIComponent(childId)}/points`, { signal }))
  },
  async childPointsLedger(childId, signal) {
    return toItems(await api.get(`/api/v1/students/${encodeURIComponent(childId)}/points/ledger?pageSize=50`, { signal }))
  },

  // ---- document requests (parent creates + tracks; staff process) ----
  async documentRequests(signal) {
    return toItems(await api.get('/api/v1/parent-requests', { signal }))
  },
  async documentRequest(id, signal) {
    return toObject(await api.get(`/api/v1/parent-requests/${encodeURIComponent(id)}`, { signal }))
  },
  async createDocumentRequest(body) {
    // body: { studentId, type, title, body } — type is a ParentRequestType name/value.
    return unwrapEnvelope(await api.post('/api/v1/parent-requests', body))
  },
  // ---- Phase 19: durable sensitive-document attachment for a parent request ----
  // Uploads through the dedicated, relationship-authorized backend endpoint (NOT the
  // generic file API); the bytes never touch object storage from the browser.
  async attachRequestDocument(id, file) {
    const form = new FormData()
    form.append('File', file)
    return unwrapEnvelope(await api.upload(`/api/v1/parent-requests/${encodeURIComponent(id)}/attachment`, form))
  },
  // Backend-mediated, audited download of the request's attached document.
  downloadRequestDocument(id, fallbackName = 'document') {
    return downloadToBrowser(`/api/v1/parent-requests/${encodeURIComponent(id)}/attachment/download`, fallbackName)
  },
  // Backend-mediated, audited download of a staff response's attached document.
  // ParentRequestResponseDto carries no "has a document" flag, so the caller
  // attempts this per response; the backend honestly 404s when there is none.
  downloadResponseDocument(id, responseId, fallbackName = 'response-document') {
    return downloadToBrowser(`/api/v1/parent-requests/${encodeURIComponent(id)}/responses/${encodeURIComponent(responseId)}/document/download`, fallbackName)
  },

  // ---- teacher conversations (read; participant-scoped server-side) ----
  async conversations(signal) {
    return toItems(await api.get('/api/v1/conversations', { signal }))
  },

  // ---- shared inbox + announcements ----
  async notifications(signal) {
    const [items, count] = await Promise.allSettled([
      api.get('/api/v1/notifications', { signal }),
      api.get('/api/v1/notifications/unread-count', { signal }),
    ])
    return { items, count }
  },
  async markNotificationRead(id) {
    return api.patch(`/api/v1/notifications/${encodeURIComponent(id)}/read`)
  },
  async announcements(signal) {
    return toItems(await api.get('/api/v1/announcements', { signal }))
  },
}
