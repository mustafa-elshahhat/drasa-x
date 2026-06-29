// =============================================================================
// Phase 9 — Teacher Portal data access. Uses the SINGLE canonical API client
// (DerasaX-backend only); the browser never calls school-ai-rag. AI quiz drafts
// are generated via the backend-mediated endpoint and are always DRAFTS that the
// teacher must review and explicitly publish/assign. Every response is normalized
// through the shared envelope helpers.
// =============================================================================
import { api } from '../../lib/api/client'
import { toItems, toObject, unwrapEnvelope } from '../student/studentSchemas'

function qs(params = {}) {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  }
  const text = search.toString()
  return text ? `?${text}` : ''
}

export const teacherApi = {
  // ---- teacher-scoped summary + roster (Phase 9 backend contract) ----
  async dashboard(signal) {
    return toObject(await api.get('/api/v1/teacher/dashboard', { signal }))
  },
  async classes(signal) {
    return toItems(await api.get('/api/v1/teacher/classes', { signal }))
  },
  async subjects(signal) {
    return toItems(await api.get('/api/v1/teacher/subjects', { signal }))
  },
  async classStudents(classId, signal) {
    return toItems(await api.get(`/api/v1/teacher/classes/${encodeURIComponent(classId)}/students`, { signal }))
  },
  async myStudents(signal) {
    return toItems(await api.get('/api/v1/me/students', { signal }))
  },

  // ---- curriculum browse (shared read endpoints) ----
  async subjectUnits(subjectId, signal) {
    return toItems(await api.get(`/api/Units/GetUnitsBySubjectId${qs({ id: subjectId })}`, { signal }))
  },
  async unitLessons(unitId, signal) {
    return toItems(await api.get(`/api/Lessons/GetLessonsByUnitId${qs({ id: unitId })}`, { signal }))
  },
  async lessonMaterials(lessonId, signal) {
    return toItems(await api.get(`/api/LessonMaterial/GetMaterialByLessonId${qs({ id: lessonId })}`, { signal }))
  },

  // ---- quiz authoring lifecycle ----
  async quizzes(signal) {
    return toItems(await api.get('/api/v1/quizzes', { signal }))
  },
  async quiz(quizId, signal) {
    return toObject(await api.get(`/api/v1/quizzes/${encodeURIComponent(quizId)}`, { signal }))
  },
  async quizSubmissions(quizId, signal) {
    return toItems(await api.get(`/api/v1/quizzes/${encodeURIComponent(quizId)}/submissions`, { signal }))
  },
  async quizAssignments(quizId, signal) {
    return toItems(await api.get(`/api/v1/quizzes/${encodeURIComponent(quizId)}/assignments`, { signal }))
  },
  async quizAnalytics(quizId, signal) {
    return toObject(await api.get(`/api/v1/quizzes/${encodeURIComponent(quizId)}/analytics`, { signal }))
  },

  // AI draft generation — backend-mediated, draft-only.
  async generateDraft(payload) {
    return unwrapEnvelope(await api.post('/api/v1/ai/quiz/draft', payload))
  },
  async addQuestion(quizId, body) {
    return unwrapEnvelope(await api.post(`/api/v1/quizzes/${encodeURIComponent(quizId)}/questions`, body))
  },
  async updateQuestion(quizId, questionId, body) {
    return unwrapEnvelope(await api.put(`/api/v1/quizzes/${encodeURIComponent(quizId)}/questions/${encodeURIComponent(questionId)}`, body))
  },
  async deleteQuestion(quizId, questionId) {
    return unwrapEnvelope(await api.del(`/api/v1/quizzes/${encodeURIComponent(quizId)}/questions/${encodeURIComponent(questionId)}`))
  },
  async publishQuiz(quizId) {
    return unwrapEnvelope(await api.post(`/api/v1/quizzes/${encodeURIComponent(quizId)}/publish`))
  },
  async archiveQuiz(quizId) {
    return unwrapEnvelope(await api.post(`/api/v1/quizzes/${encodeURIComponent(quizId)}/archive`))
  },
  async assignQuiz(quizId, body) {
    return unwrapEnvelope(await api.post(`/api/v1/quizzes/${encodeURIComponent(quizId)}/assignments`, body))
  },

  // ---- grading + feedback ----
  async submission(attemptId, signal) {
    return toObject(await api.get(`/api/v1/submissions/${encodeURIComponent(attemptId)}`, { signal }))
  },
  async gradeSubmission(attemptId, grades) {
    return unwrapEnvelope(await api.post(`/api/v1/submissions/${encodeURIComponent(attemptId)}/grade`, { grades }))
  },
  async feedbackSubmission(attemptId, feedback) {
    return unwrapEnvelope(await api.post(`/api/v1/submissions/${encodeURIComponent(attemptId)}/feedback`, { feedback }))
  },

  // ---- assigned-student analytics (assignment-gated server-side) ----
  async studentProgress(studentId, signal) {
    const base = `/api/v1/students/${encodeURIComponent(studentId)}`
    const [summary, painPoints, insights, recommendations] = await Promise.allSettled([
      api.get(`${base}/progress-summary`, { signal }),
      api.get(`${base}/pain-points`, { signal }),
      api.get(`${base}/insights`, { signal }),
      api.get(`${base}/recommendations`, { signal }),
    ])
    return { summary, painPoints, insights, recommendations }
  },

  // ---- shared inbox + office hours ----
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
  async officeHours(signal) {
    return toItems(await api.get('/api/v1/office-hours/mine', { signal }))
  },
  async markAttendance(bookingId, status) {
    return unwrapEnvelope(await api.post(`/api/v1/bookings/${encodeURIComponent(bookingId)}/attendance`, { status }))
  },

  // ---- Phase 14: ledger-based gamification (teacher/admin authoring) ----
  async studentPoints(studentId, signal) {
    return toObject(await api.get(`/api/v1/students/${encodeURIComponent(studentId)}/points`, { signal }))
  },
  async studentPointsLedger(studentId, signal) {
    return toItems(await api.get(`/api/v1/students/${encodeURIComponent(studentId)}/points/ledger${qs({ pageSize: 50 })}`, { signal }))
  },
  async awardPoints(studentId, { points, reason, idempotencyKey } = {}) {
    return unwrapEnvelope(await api.post(`/api/v1/students/${encodeURIComponent(studentId)}/points`, { points, reason, idempotencyKey }))
  },
  async pointsLeaderboard({ gradeId } = {}, signal) {
    return toItems(await api.get(`/api/v1/gamification/leaderboard${qs({ gradeId, pageSize: 50 })}`, { signal }))
  },
  async gamificationRules(signal) {
    return toItems(await api.get('/api/v1/gamification/rules', { signal }))
  },
  async upsertGamificationRule(rule) {
    return unwrapEnvelope(await api.put('/api/v1/gamification/rules', rule))
  },
}
