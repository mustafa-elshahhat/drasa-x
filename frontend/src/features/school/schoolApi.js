// =============================================================================
// Phase 11 — School Admin Portal data access. Uses the SINGLE canonical API client
// (DerasaX-backend only); the browser never calls school-ai-rag. Every endpoint is
// tenant-scoped + SchoolAdmin-guarded server-side — the UI only renders what the
// backend returns and fabricates no statistics, ids, or records.
//
// Most pages reuse the existing Phase 5 SchoolAdmin contracts; the aggregate
// dashboard, parent↔student relationship management and teacher↔class assignment
// management use the NEW Phase 11 `/api/v1/school-admin/...` contracts.
//
// Enums are bound NUMERICALLY by the backend (no JsonStringEnumConverter), so write
// payloads send numeric enum values (see the *_VALUE maps in the page component).
// =============================================================================
import { api } from '../../lib/api/client'
import { toItems, toObject, unwrapEnvelope } from '../student/studentSchemas'

const q = (params) => {
  const usp = new URLSearchParams()
  Object.entries(params || {}).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') usp.append(k, v)
  })
  const s = usp.toString()
  return s ? `?${s}` : ''
}

export const schoolApi = {
  // ---- aggregate dashboard (Phase 11 contract) ----
  async dashboard(signal) {
    return toObject(await api.get('/api/v1/school-admin/dashboard', { signal }))
  },

  // ---- tenant profile / subscription / usage (reused) ----
  async profile(signal) {
    return toObject(await api.get('/api/v1/my-tenant', { signal }))
  },
  async subscription(signal) {
    return toObject(await api.get('/api/v1/my-tenant/subscription', { signal }))
  },
  async usage(signal) {
    return toObject(await api.get('/api/v1/my-tenant/usage', { signal }))
  },

  // ---- academic structure ----
  async academicYears(signal) {
    return toItems(await api.get('/api/v1/academic-years', { signal }))
  },
  async createAcademicYear(body) {
    return unwrapEnvelope(await api.post('/api/v1/academic-years', body))
  },
  async terms(signal) {
    return toItems(await api.get('/api/v1/terms', { signal }))
  },
  async createTerm(body) {
    return unwrapEnvelope(await api.post('/api/v1/terms', body))
  },
  async grades(signal) {
    return toItems(await api.get('/api/Grades/GetAllGrades', { signal }))
  },
  async createGrade(body) {
    return unwrapEnvelope(await api.post('/api/Grades/AddGrade', body))
  },
  async classes(signal) {
    return toItems(await api.get('/api/v1/classes', { signal }))
  },
  async createClass(body) {
    return unwrapEnvelope(await api.post('/api/v1/classes', body))
  },
  async subjects(signal) {
    return toItems(await api.get('/api/Subjects/GetSubjects', { signal }))
  },

  // ---- tenant users (reused provisioning contract) ----
  async users(role, signal) {
    // Active accounts only — disabled accounts are not valid link/assignment targets
    // (the backend rejects them with 404), so they are not offered in any picker.
    return toItems(await api.get(`/api/v1/tenant-users${q({ role, pageSize: 100 })}`, { signal }))
  },
  async createUser(body) {
    // body: { fullName, loginCode, role, gradeId? } — returns a one-time credential.
    return unwrapEnvelope(await api.post('/api/v1/tenant-users', body))
  },
  async setUserEnabled(id, enabled) {
    return api.post(`/api/v1/tenant-users/${encodeURIComponent(id)}/${enabled ? 'enable' : 'disable'}`)
  },

  // ---- parent↔student relationships (Phase 11 contract) ----
  async relationships(signal) {
    return toItems(await api.get(`/api/v1/school-admin/relationships${q({ activeOnly: false, pageSize: 100 })}`, { signal }))
  },
  async createRelationship(body) {
    return unwrapEnvelope(await api.post('/api/v1/school-admin/relationships', body))
  },
  async deactivateRelationship(id) {
    return api.post(`/api/v1/school-admin/relationships/${encodeURIComponent(id)}/deactivate`)
  },

  // ---- teacher assignments (subject: reused; class: Phase 11 contract) ----
  async subjectAssignments(signal) {
    return toItems(await api.get('/api/v1/teacher-subject-assignments', { signal }))
  },
  async createSubjectAssignment(body) {
    return unwrapEnvelope(await api.post('/api/v1/teacher-subject-assignments', body))
  },
  async classAssignments(signal) {
    return toItems(await api.get(`/api/v1/school-admin/teacher-class-assignments${q({ activeOnly: false, pageSize: 100 })}`, { signal }))
  },
  async createClassAssignment(body) {
    return unwrapEnvelope(await api.post('/api/v1/school-admin/teacher-class-assignments', body))
  },

  // ---- announcements (reused) ----
  async announcements(signal) {
    return toItems(await api.get('/api/v1/announcements', { signal }))
  },
  async createAnnouncement(body) {
    return unwrapEnvelope(await api.post('/api/v1/announcements', body))
  },
  async publishAnnouncement(id, publish) {
    return api.post(`/api/v1/announcements/${encodeURIComponent(id)}/publish${q({ publish })}`)
  },

  // ---- parent document requests (reused; SchoolAdmin responds/transitions) ----
  async documentRequests(signal) {
    return toItems(await api.get('/api/v1/parent-requests', { signal }))
  },
  async respondDocumentRequest(id, body) {
    return unwrapEnvelope(await api.post(`/api/v1/parent-requests/${encodeURIComponent(id)}/responses`, body))
  },
  async transitionDocumentRequest(id, body) {
    return unwrapEnvelope(await api.post(`/api/v1/parent-requests/${encodeURIComponent(id)}/status`, body))
  },

  // ---- engagement (reused) ----
  async communities(signal) {
    return toItems(await api.get('/api/v1/communities', { signal }))
  },
  async competitions(signal) {
    return toItems(await api.get('/api/v1/competitions', { signal }))
  },

  // ---- reports / ai usage / audit / support / settings (reused) ----
  async reports(signal) {
    const [users, ai] = await Promise.allSettled([
      api.get('/api/v1/reports/tenant-users', { signal }),
      api.get('/api/v1/reports/ai-usage-activity', { signal }),
    ])
    return { users, ai }
  },
  async aiUsage(signal) {
    const [summary, list] = await Promise.allSettled([
      api.get('/api/v1/ai-usage/summary', { signal }),
      api.get('/api/v1/ai-usage', { signal }),
    ])
    return { summary, list }
  },
  async support(signal) {
    return toItems(await api.get('/api/v1/support-requests', { signal }))
  },
  async respondSupport(id, body) {
    return unwrapEnvelope(await api.post(`/api/v1/support-requests/${encodeURIComponent(id)}/respond`, body))
  },
  async audit(signal) {
    return toItems(await api.get('/api/v1/audit', { signal }))
  },
  async settings(signal) {
    return toItems(await api.get('/api/v1/tenant-settings', { signal }))
  },
  async upsertSetting(body) {
    return unwrapEnvelope(await api.put('/api/v1/tenant-settings', body))
  },
}
