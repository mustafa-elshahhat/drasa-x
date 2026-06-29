import { api } from '../../lib/api/client'
import { normalizeAttendance, normalizeTutorResponse, toItems, toObject, unwrapEnvelope } from './studentSchemas'

function qs(params = {}) {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  }
  const text = search.toString()
  return text ? `?${text}` : ''
}

export const studentApi = {
  async subjects(signal) {
    return toItems(await api.get('/api/v1/Subjects/GetSubjects', { signal }))
  },
  async subject(id, signal) {
    return toObject(await api.get(`/api/v1/Subjects/GetSubjectById/${encodeURIComponent(id)}`, { signal }))
  },
  async units(subjectId, signal) {
    return toItems(await api.get(`/api/v1/Units/GetUnitsBySubjectId${qs({ id: subjectId })}`, { signal }))
  },
  async lessons(unitId, signal) {
    return toItems(await api.get(`/api/v1/Lessons/GetLessonsByUnitId${qs({ id: unitId })}`, { signal }))
  },
  async materials(lessonId, signal) {
    return toItems(await api.get(`/api/v1/LessonMaterial/GetMaterialByLessonId${qs({ id: lessonId })}`, { signal }))
  },
  async completeLesson(lessonId) {
    return unwrapEnvelope(await api.post(`/api/v1/student/lessons/${encodeURIComponent(lessonId)}/complete`))
  },
  async resourceComments(materialId, signal) {
    return toItems(await api.get(`/api/v1/lesson-materials/${encodeURIComponent(materialId)}/comments`, { signal }))
  },
  async addResourceComment(materialId, body) {
    return unwrapEnvelope(await api.post(`/api/v1/lesson-materials/${encodeURIComponent(materialId)}/comments`, { body }))
  },
  async homework(signal) {
    return toItems(await api.get('/api/v1/homework/assigned', { signal }))
  },
  async homeworkSubmission(homeworkId, signal) {
    return toObject(await api.get(`/api/v1/homework/${encodeURIComponent(homeworkId)}/my-submission`, { signal }))
  },
  async submitHomework(homeworkId, content) {
    return unwrapEnvelope(await api.post(`/api/v1/homework/${encodeURIComponent(homeworkId)}/submit`, { content }))
  },
  async assignedQuizzes(signal) {
    return toItems(await api.get('/api/v1/assigned-quizzes', { signal }))
  },
  async startQuiz(quizId) {
    return unwrapEnvelope(await api.post(`/api/v1/quizzes/${encodeURIComponent(quizId)}/attempts`))
  },
  async attempt(attemptId, signal) {
    return toObject(await api.get(`/api/v1/attempts/${encodeURIComponent(attemptId)}`, { signal }))
  },
  async saveAttempt(attemptId, answers) {
    return unwrapEnvelope(await api.put(`/api/v1/attempts/${encodeURIComponent(attemptId)}/answers`, { answers }))
  },
  async submitAttempt(attemptId) {
    return unwrapEnvelope(await api.post(`/api/v1/attempts/${encodeURIComponent(attemptId)}/submit`))
  },
  async attemptResult(attemptId, signal) {
    return toObject(await api.get(`/api/v1/attempts/${encodeURIComponent(attemptId)}/result`, { signal }))
  },
  async previousAttempts(quizId, signal) {
    return toItems(await api.get(`/api/v1/quizzes/${encodeURIComponent(quizId)}/my-attempts`, { signal }))
  },
  async tutor(payload) {
    return normalizeTutorResponse(await api.post('/api/v1/ai/tutor', payload))
  },
  async progress(studentId, signal) {
    const base = `/api/v1/students/${encodeURIComponent(studentId)}`
    const [summary, subjects, lessons, metrics, attempts, insights, painPoints, recommendations, predictions] = await Promise.allSettled([
      api.get(`${base}/progress-summary`, { signal }),
      api.get(`${base}/subject-progress`, { signal }),
      api.get(`${base}/lesson-progress`, { signal }),
      api.get(`${base}/metric-history`, { signal }),
      api.get(`${base}/attempt-history`, { signal }),
      api.get(`${base}/insights`, { signal }),
      api.get(`${base}/pain-points`, { signal }),
      api.get(`${base}/recommendations`, { signal }),
      api.get(`${base}/predictions`, { signal }),
    ])
    return { summary, subjects, lessons, metrics, attempts, insights, painPoints, recommendations, predictions }
  },
  async attendance(signal) {
    return normalizeAttendance(await api.get('/api/v1/student/attendance', { signal }))
  },
  async communities(signal) {
    return toItems(await api.get('/api/v1/communities', { signal }))
  },
  async community(id, signal) {
    return toObject(await api.get(`/api/v1/communities/${encodeURIComponent(id)}`, { signal }))
  },
  async communityPosts(id, signal) {
    return toItems(await api.get(`/api/v1/communities/${encodeURIComponent(id)}/posts`, { signal }))
  },
  async joinCommunity(id) {
    return unwrapEnvelope(await api.post(`/api/v1/communities/${encodeURIComponent(id)}/join`))
  },
  async leaveCommunity(id) {
    return unwrapEnvelope(await api.post(`/api/v1/communities/${encodeURIComponent(id)}/leave`))
  },
  async createCommunityPost(id, content) {
    return unwrapEnvelope(await api.post(`/api/v1/communities/${encodeURIComponent(id)}/posts`, { content }))
  },
  async commentOnPost(postId, body) {
    return unwrapEnvelope(await api.post(`/api/v1/posts/${encodeURIComponent(postId)}/comments`, { body }))
  },
  async competitions(signal) {
    return toItems(await api.get('/api/v1/competitions', { signal }))
  },
  async competition(id, signal) {
    return toObject(await api.get(`/api/v1/competitions/${encodeURIComponent(id)}`, { signal }))
  },
  async enterCompetition(id) {
    return unwrapEnvelope(await api.post(`/api/v1/competitions/${encodeURIComponent(id)}/entries`))
  },
  // Phase 14 (closure) — durable competition submissions for the entered student.
  // A 404 means "not submitted yet" (and is also returned before entering) — surfaced as null.
  async competitionSubmission(id, signal) {
    try {
      return toObject(await api.get(`/api/v1/competitions/${encodeURIComponent(id)}/submissions/me`, { signal }))
    } catch (error) {
      if (error?.status === 404) return null
      throw error
    }
  },
  async submitCompetition(id, content) {
    return unwrapEnvelope(await api.post(`/api/v1/competitions/${encodeURIComponent(id)}/submissions`, { content }))
  },
  async leaderboard(id, signal) {
    return toItems(await api.get(`/api/v1/competitions/${encodeURIComponent(id)}/leaderboard`, { signal }))
  },
  async officeHours(signal) {
    const [available, mine] = await Promise.allSettled([
      api.get('/api/v1/office-hours/available', { signal }),
      api.get('/api/v1/office-hours/mine', { signal }),
    ])
    return { available, mine }
  },
  async bookOfficeHour(id, notes) {
    return unwrapEnvelope(await api.post(`/api/v1/office-hours/${encodeURIComponent(id)}/bookings`, { notes }))
  },
  async cancelBooking(id) {
    return unwrapEnvelope(await api.post(`/api/v1/bookings/${encodeURIComponent(id)}/cancel`))
  },
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
  async markAllNotificationsRead() {
    return api.patch('/api/v1/notifications/read-all')
  },
  async announcements(signal) {
    return toItems(await api.get('/api/v1/announcements', { signal }))
  },
  async submitSuggestion(title, body) {
    return unwrapEnvelope(await api.post('/api/v1/suggestions', { title, body }))
  },
  async badges(studentId, signal) {
    const base = `/api/v1/students/${encodeURIComponent(studentId)}`
    const [catalog, earned, streak] = await Promise.allSettled([
      api.get('/api/v1/badges', { signal }),
      api.get(`${base}/badges`, { signal }),
      api.get(`${base}/streak`, { signal }),
    ])
    return { catalog, earned, streak }
  },
  // Phase 14 — ledger-based gamification (read-only for the student).
  async points(studentId, signal) {
    return toObject(await api.get(`/api/v1/students/${encodeURIComponent(studentId)}/points`, { signal }))
  },
  async pointsLedger(studentId, signal) {
    return toItems(await api.get(`/api/v1/students/${encodeURIComponent(studentId)}/points/ledger${qs({ pageSize: 50 })}`, { signal }))
  },
  async pointsLeaderboard({ gradeId } = {}, signal) {
    return toItems(await api.get(`/api/v1/gamification/leaderboard${qs({ gradeId, pageSize: 50 })}`, { signal }))
  },
}
