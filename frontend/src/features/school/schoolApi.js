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
  async requestRenewal(body) {
    return unwrapEnvelope(await api.post('/api/v1/my-tenant/renewal-requests', body))
  },

  // ---- academic structure ----
  async academicYears(signal) {
    return toItems(await api.get('/api/v1/academic-years', { signal }))
  },
  async createAcademicYear(body) {
    return unwrapEnvelope(await api.post('/api/v1/academic-years', body))
  },
  async updateAcademicYear(id, body) {
    return unwrapEnvelope(await api.put(`/api/v1/academic-years/${encodeURIComponent(id)}`, body))
  },
  async archiveAcademicYear(id) {
    return api.del(`/api/v1/academic-years/${encodeURIComponent(id)}`)
  },
  async terms(signal) {
    return toItems(await api.get('/api/v1/terms', { signal }))
  },
  async createTerm(body) {
    return unwrapEnvelope(await api.post('/api/v1/terms', body))
  },
  async updateTerm(id, body) {
    return unwrapEnvelope(await api.put(`/api/v1/terms/${encodeURIComponent(id)}`, body))
  },
  async archiveTerm(id) {
    return api.del(`/api/v1/terms/${encodeURIComponent(id)}`)
  },
  async grades(signal) {
    return toItems(await api.get('/api/v1/Grades/GetAllGrades', { signal }))
  },
  async createGrade(body) {
    return unwrapEnvelope(await api.post('/api/v1/Grades/AddGrade', body))
  },
  async updateGrade(body) {
    // body: { id, name } — the backend route carries no {id} segment (id is in the body);
    // ASP.NET Core's default JSON binding is case-insensitive, and this codebase's other
    // working PUT bodies (e.g. updateClass) send camelCase, so this matches that convention.
    return unwrapEnvelope(await api.put('/api/v1/Grades/UpdateGrade', body))
  },
  async deleteGrade(id) {
    // Hard delete (not an archive) — id is a query-string param, not a route segment.
    return api.del(`/api/v1/Grades/DeleteGrade${q({ id })}`)
  },
  async classes(signal) {
    return toItems(await api.get('/api/v1/classes', { signal }))
  },
  async getClass(id, signal) {
    return toObject(await api.get(`/api/v1/classes/${encodeURIComponent(id)}`, { signal }))
  },
  async createClass(body) {
    return unwrapEnvelope(await api.post('/api/v1/classes', body))
  },
  async updateClass(id, body) {
    return unwrapEnvelope(await api.put(`/api/v1/classes/${encodeURIComponent(id)}`, body))
  },

  // ---- enrollments (Phase 5 §9.1 contract) ----
  async enrollments(classId, signal) {
    return toItems(await api.get(`/api/v1/enrollments${q({ schoolClassId: classId, pageSize: 200 })}`, { signal }))
  },
  async enrollStudent(body) {
    return unwrapEnvelope(await api.post('/api/v1/enrollments', body))
  },
  async withdrawEnrollment(id, reason) {
    return unwrapEnvelope(await api.post(`/api/v1/enrollments/${encodeURIComponent(id)}/withdraw`, { reason: reason || null }))
  },
  async subjects(signal) {
    return toItems(await api.get('/api/v1/Subjects/GetSubjects', { signal }))
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
  async getUser(id, signal) {
    return toObject(await api.get(`/api/v1/tenant-users/${encodeURIComponent(id)}`, { signal }))
  },
  async setUserEnabled(id, enabled) {
    return unwrapEnvelope(await api.post(`/api/v1/tenant-users/${encodeURIComponent(id)}/${enabled ? 'enable' : 'disable'}`))
  },
  async resetUserCredential(id) {
    // Returns a fresh one-time credential { loginCode, temporaryPassword } — same
    // shape as createUser's response.
    return unwrapEnvelope(await api.post(`/api/v1/tenant-users/${encodeURIComponent(id)}/reset-credential`))
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
  async deactivateClassAssignment(id) {
    return unwrapEnvelope(await api.post(`/api/v1/school-admin/teacher-class-assignments/${encodeURIComponent(id)}/deactivate`))
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
  // Phase 16 — staff responds to a request with a sensitive document (multipart; complements
  // the text-only respondDocumentRequest above rather than replacing it).
  async attachResponseDocument(id, file, body) {
    const form = new FormData()
    form.append('File', file)
    if (body) form.append('Body', body)
    return unwrapEnvelope(await api.upload(`/api/v1/parent-requests/${encodeURIComponent(id)}/response-document`, form))
  },

  // ---- engagement: communities (fix pass — full CRUD/moderation; NEW SchoolAdmin management UI
  // replaces the former read-only dump. CommunitiesController is [Authorize(TenantMember)] with
  // service-layer role/membership checks — SchoolAdmin has a blanket bypass on every write below,
  // so the UI does not need to replicate the Owner/Moderator membership gate for its own role. ----
  async communities(signal) {
    return toItems(await api.get('/api/v1/communities', { signal }))
  },
  async getCommunity(id, signal) {
    return toObject(await api.get(`/api/v1/communities/${encodeURIComponent(id)}`, { signal }))
  },
  async createCommunity(body) {
    return unwrapEnvelope(await api.post('/api/v1/communities', body))
  },
  async updateCommunity(id, body) {
    return unwrapEnvelope(await api.put(`/api/v1/communities/${encodeURIComponent(id)}`, body))
  },
  async archiveCommunity(id) {
    return unwrapEnvelope(await api.post(`/api/v1/communities/${encodeURIComponent(id)}/archive`))
  },
  async communityMembers(id, signal) {
    return toItems(await api.get(`/api/v1/communities/${encodeURIComponent(id)}/members`, { signal }))
  },
  async addCommunityMember(id, body) {
    return unwrapEnvelope(await api.post(`/api/v1/communities/${encodeURIComponent(id)}/members`, body))
  },
  async communityPosts(id, signal) {
    return toItems(await api.get(`/api/v1/communities/${encodeURIComponent(id)}/posts`, { signal }))
  },
  async createCommunityPost(id, body) {
    return unwrapEnvelope(await api.post(`/api/v1/communities/${encodeURIComponent(id)}/posts`, body))
  },
  async deleteCommunityPost(postId) {
    return unwrapEnvelope(await api.del(`/api/v1/posts/${encodeURIComponent(postId)}`))
  },
  // No comment-list endpoint exists on the backend (only create/delete-by-id), so these two are
  // exposed for contract completeness but have no consuming UI — see SchoolCommunitiesPage's note.
  async createPostComment(postId, body) {
    return unwrapEnvelope(await api.post(`/api/v1/posts/${encodeURIComponent(postId)}/comments`, body))
  },
  async deleteComment(commentId) {
    return unwrapEnvelope(await api.del(`/api/v1/comments/${encodeURIComponent(commentId)}`))
  },
  async reportPost(postId, body) {
    return unwrapEnvelope(await api.post(`/api/v1/posts/${encodeURIComponent(postId)}/reports`, body))
  },
  async moderatePost(postId, body) {
    return unwrapEnvelope(await api.post(`/api/v1/posts/${encodeURIComponent(postId)}/moderate`, body))
  },
  async competitions(signal) {
    return toItems(await api.get('/api/v1/competitions', { signal }))
  },

  // ---- reports / ai usage / audit / support / settings (reused) ----
  async reports(signal) {
    const [users, ai, assessmentSummary, auditActivity] = await Promise.allSettled([
      api.get('/api/v1/reports/tenant-users', { signal }),
      api.get('/api/v1/reports/ai-usage-activity', { signal }),
      api.get('/api/v1/reports/assessment-summary', { signal }),
      api.get('/api/v1/reports/audit-activity', { signal }),
    ])
    return { users, ai, assessmentSummary, auditActivity }
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

  // ---- suggestions (Phase 5 §12; anonymous submission — SchoolAdmin lists + moderates only;
  // the backend never returns author identity through this surface) ----
  async suggestions(signal) {
    return toItems(await api.get('/api/v1/suggestions', { signal }))
  },
  async moderateSuggestion(id, body) {
    return unwrapEnvelope(await api.post(`/api/v1/suggestions/${encodeURIComponent(id)}/moderate`, body))
  },

  // ---- gamification rules (Phase 14 GamificationController; fix pass) — GET is readable by
  // Teacher or SchoolAdmin, but PUT (upsert-by-Code) is SchoolAdmin-exclusive server-side, so the
  // rule-editor UI lives here rather than on the pre-existing (dead) teacherApi equivalent. ----
  async gamificationRules(signal) {
    return toItems(await api.get('/api/v1/gamification/rules', { signal }))
  },
  async upsertGamificationRule(rule) {
    return unwrapEnvelope(await api.put('/api/v1/gamification/rules', rule))
  },
}
