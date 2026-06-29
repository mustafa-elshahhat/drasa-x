// =============================================================================
// Phase 15 — Computer-vision attendance + engagement API client.
//
// Backend-only: every call goes through the canonical `api` client to
// DerasaX-backend. The browser NEVER calls the AI service directly — the backend
// mediates all CV inference. Responses are unwrapped from the ApiResponse
// envelope via toObject/toItems.
// =============================================================================
import { api } from '../../lib/api/client'
import { toItems, toObject } from '../student/studentSchemas'

const BASE = '/api/v1/vision'

function qs(params = {}) {
  const s = new URLSearchParams(
    Object.entries(params).filter(([, v]) => v !== undefined && v !== null && v !== ''),
  ).toString()
  return s ? `?${s}` : ''
}

export const visionApi = {
  // ---- staff (teacher / school-admin) ----
  async startSession(payload, signal) {
    return toObject(await api.post(`${BASE}/sessions`, payload, { signal }))
  },
  async listSessions(params = {}, signal) {
    return toItems(await api.get(`${BASE}/sessions${qs(params)}`, { signal }))
  },
  async getSession(sessionId, signal) {
    return toObject(await api.get(`${BASE}/sessions/${sessionId}`, { signal }))
  },
  async endSession(sessionId) {
    return toObject(await api.post(`${BASE}/sessions/${sessionId}/end`, null))
  },
  async analyzeFrame(sessionId, payload) {
    return toObject(await api.post(`${BASE}/sessions/${sessionId}/analyze`, payload))
  },
  async listFrames(sessionId, signal) {
    return toItems(await api.get(`${BASE}/sessions/${sessionId}/frames`, { signal }))
  },
  async listCandidates(sessionId, params = {}, signal) {
    return toItems(await api.get(`${BASE}/sessions/${sessionId}/candidates${qs(params)}`, { signal }))
  },
  async getSummary(sessionId, signal) {
    return toObject(await api.get(`${BASE}/sessions/${sessionId}/summary`, { signal }))
  },
  async confirmCandidate(candidateId, payload) {
    return toObject(await api.post(`${BASE}/candidates/${candidateId}/confirm`, payload))
  },
  async rejectCandidate(candidateId, payload) {
    return toObject(await api.post(`${BASE}/candidates/${candidateId}/reject`, payload))
  },
  async overrideCandidate(candidateId, payload) {
    return toObject(await api.post(`${BASE}/candidates/${candidateId}/override`, payload))
  },
  async enrollFace(payload) {
    return toObject(await api.post(`${BASE}/enrollments`, payload))
  },
  async listEnrollments(signal) {
    return toItems(await api.get(`${BASE}/enrollments`, { signal }))
  },

  // ---- student (own data only) ----
  async myEngagementSummary(signal) {
    return toObject(await api.get('/api/v1/student/vision/engagement-summary', { signal }))
  },

  // ---- parent (linked child only) ----
  async childEngagementSummary(childId, signal) {
    return toObject(await api.get(`/api/v1/parent/vision/children/${childId}/engagement-summary`, { signal }))
  },
}

/** Read a File into a base64 data URL the backend forwards to the AI service. */
export function fileToBase64(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(String(reader.result || ''))
    reader.onerror = () => reject(reader.error || new Error('file read failed'))
    reader.readAsDataURL(file)
  })
}
