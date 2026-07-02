// =============================================================================
// Phase 9 — Teacher Portal data access. Uses the SINGLE canonical API client
// (DerasaX-backend only); the browser never calls school-ai-rag. AI quiz drafts
// are generated via the backend-mediated endpoint and are always DRAFTS that the
// teacher must review and explicitly publish/assign. Every response is normalized
// through the shared envelope helpers.
// =============================================================================
import { api, apiJson } from '../../lib/api/client'
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
    return toItems(await api.get(`/api/v1/Units/GetUnitsBySubjectId${qs({ id: subjectId })}`, { signal }))
  },
  async unitLessons(unitId, signal) {
    return toItems(await api.get(`/api/v1/Lessons/GetLessonsByUnitId${qs({ id: unitId })}`, { signal }))
  },
  async lessonMaterials(lessonId, signal) {
    return toItems(await api.get(`/api/v1/LessonMaterial/GetMaterialByLessonId${qs({ id: lessonId })}`, { signal }))
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

  // Manual quiz authoring (create + metadata edit) — complements AI draft generation.
  async createQuiz(body) {
    return unwrapEnvelope(await api.post('/api/v1/quizzes', body))
  },
  async updateQuiz(quizId, body) {
    return unwrapEnvelope(await api.put(`/api/v1/quizzes/${encodeURIComponent(quizId)}`, body))
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

  // ---- homework / non-quiz assignment lifecycle (backend complete; Phase 4) ----
  async homeworkList(signal) {
    return toItems(await api.get('/api/v1/homework', { signal }))
  },
  async homework(homeworkId, signal) {
    return toObject(await api.get(`/api/v1/homework/${encodeURIComponent(homeworkId)}`, { signal }))
  },
  async createHomework(body) {
    return unwrapEnvelope(await api.post('/api/v1/homework', body))
  },
  async updateHomework(homeworkId, body) {
    return unwrapEnvelope(await api.put(`/api/v1/homework/${encodeURIComponent(homeworkId)}`, body))
  },
  async publishHomework(homeworkId, body) {
    return unwrapEnvelope(await api.post(`/api/v1/homework/${encodeURIComponent(homeworkId)}/publish`, body))
  },
  async homeworkSubmissions(homeworkId, signal) {
    return toItems(await api.get(`/api/v1/homework/${encodeURIComponent(homeworkId)}/submissions${qs({ pageSize: 100 })}`, { signal }))
  },
  async gradeHomeworkSubmission(submissionId, body) {
    return unwrapEnvelope(await api.post(`/api/v1/homework/submissions/${encodeURIComponent(submissionId)}/grade`, body))
  },

  // ---- competition lifecycle (teacher/admin; backend complete; Phase 6) ----
  async competitions(signal) {
    return toItems(await api.get('/api/v1/competitions', { signal }))
  },
  async competition(competitionId, signal) {
    return toObject(await api.get(`/api/v1/competitions/${encodeURIComponent(competitionId)}`, { signal }))
  },
  async createCompetition(body) {
    return unwrapEnvelope(await api.post('/api/v1/competitions', body))
  },
  async updateCompetition(competitionId, body) {
    return unwrapEnvelope(await api.put(`/api/v1/competitions/${encodeURIComponent(competitionId)}`, body))
  },
  async publishCompetition(competitionId) {
    return unwrapEnvelope(await api.post(`/api/v1/competitions/${encodeURIComponent(competitionId)}/publish`))
  },
  async archiveCompetition(competitionId) {
    return unwrapEnvelope(await api.post(`/api/v1/competitions/${encodeURIComponent(competitionId)}/archive`))
  },
  async closeCompetition(competitionId) {
    return unwrapEnvelope(await api.post(`/api/v1/competitions/${encodeURIComponent(competitionId)}/close`))
  },
  async competitionSubmissions(competitionId, signal) {
    return toItems(await api.get(`/api/v1/competitions/${encodeURIComponent(competitionId)}/submissions`, { signal }))
  },
  async competitionLeaderboard(competitionId, signal) {
    return toItems(await api.get(`/api/v1/competitions/${encodeURIComponent(competitionId)}/leaderboard`, { signal }))
  },
  async scoreCompetitionEntry(competitionId, entryId, score) {
    return unwrapEnvelope(await api.post(`/api/v1/competitions/${encodeURIComponent(competitionId)}/entries/${encodeURIComponent(entryId)}/score`, { score }))
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
  async createOfficeHour(body) {
    return unwrapEnvelope(await api.post('/api/v1/office-hours', body))
  },
  async updateOfficeHour(id, body) {
    return unwrapEnvelope(await api.put(`/api/v1/office-hours/${encodeURIComponent(id)}`, body))
  },
  async cancelOfficeHour(id) {
    return unwrapEnvelope(await api.post(`/api/v1/office-hours/${encodeURIComponent(id)}/cancel`))
  },
  async officeHourBookings(id, signal) {
    return toItems(await api.get(`/api/v1/office-hours/${encodeURIComponent(id)}/bookings`, { signal }))
  },
  async markAttendance(bookingId, status) {
    return unwrapEnvelope(await api.post(`/api/v1/bookings/${encodeURIComponent(bookingId)}/attendance`, { status }))
  },

  // ---- engagement: communities (fix pass — brand-new Teacher creation/moderation UI;
  // CommunitiesController is [Authorize(TenantMember)] with service-layer role/membership checks.
  // Create = any Teacher/SchoolAdmin in the tenant (creator becomes Owner automatically); every
  // other write below requires the caller to be that community's Owner/Moderator (or SchoolAdmin),
  // so `communities()` returns ALL tenant communities but management actions only succeed on the
  // ones the current teacher actually manages — the UI gates by the fetched members list and
  // otherwise lets a real 403 surface via ErrorState. Same DTO shapes as schoolApi's community
  // methods (both wrap the same backend contract for their respective portals). ----
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
  // No comment-list endpoint exists on the backend (only create/delete-by-id) — exposed for
  // contract completeness but not wired into any list-driven UI (see TeacherCommunitiesPage's note).
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

  // ---- engagement: badges (fix pass — Teacher award-badge UI; BadgesController, distinct from
  // GamificationController's ledger points below). Award is gated server-side by the same
  // IStudentAccessAuthorizer relationship check as awardPoints (teacher -> assigned students only;
  // unknown/cross-tenant student id -> 404). Badge catalog is a small fixed platform-level list —
  // there is no create/edit endpoint for badges themselves. ----
  async badgeCatalog(signal) {
    return toItems(await api.get('/api/v1/badges', { signal }))
  },
  async studentBadges(studentId, signal) {
    return toItems(await api.get(`/api/v1/students/${encodeURIComponent(studentId)}/badges`, { signal }))
  },
  async awardBadge(studentId, { badgeId, reason } = {}) {
    return unwrapEnvelope(await api.post(`/api/v1/students/${encodeURIComponent(studentId)}/badges`, { badgeId, reason: reason || null }))
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

  // ---- P1-4: curriculum authoring (Teacher/SchoolAdmin) ----
  // Subjects/Units/Lessons/LessonMaterial Add + Update actions are the legacy verb-style,
  // [FromForm]-bound endpoints (multipart) — never JSON — so every add/update call below sends
  // FormData, matching AddSubjectDto/UpdateSubjectDto/AddUnitDto/UpdateUnitDto/AddLessonDto/
  // GetLessonDto exactly. `apiJson` (not the JSON-only `api.put`) is used for the two PUT calls so
  // the multipart body survives untouched. Delete* actions bind `id` from the query string (no
  // [FromForm]/[FromBody] on a plain string parameter), the same shape already proven by
  // `DeleteGrade?id=`.
  async grades(signal) {
    return toItems(await api.get('/api/v1/Grades/GetAllGrades', { signal }))
  },
  async addSubject({ name, description, gradeId, image } = {}) {
    const form = new FormData()
    form.append('Name', name)
    if (description) form.append('Description', description)
    form.append('GradeId', gradeId)
    if (image) form.append('ImageUrl', image)
    return unwrapEnvelope(await api.upload('/api/v1/Subjects/AddSubject', form))
  },
  async updateSubject(id, { name, description, gradeId, image } = {}) {
    const form = new FormData()
    form.append('Id', id)
    if (name) form.append('Name', name)
    if (description) form.append('Description', description)
    form.append('GradeId', gradeId)
    if (image) form.append('ImageUrl', image)
    return unwrapEnvelope(await apiJson('/api/v1/Subjects/UpdateSubject', { method: 'PUT', body: form }))
  },
  async deleteSubject(id) {
    return unwrapEnvelope(await api.del(`/api/v1/Subjects/DeleteSubject/${encodeURIComponent(id)}`))
  },
  async addUnit({ title, subjectId } = {}) {
    const form = new FormData()
    form.append('Title', title)
    form.append('SubjectId', subjectId)
    return unwrapEnvelope(await api.upload('/api/v1/Units/AddUnit', form))
  },
  async updateUnit(id, { title, subjectId } = {}) {
    const form = new FormData()
    form.append('Id', id)
    form.append('Title', title)
    form.append('SubjectId', subjectId)
    return unwrapEnvelope(await apiJson('/api/v1/Units/UpdateUnit', { method: 'PUT', body: form }))
  },
  async deleteUnit(id) {
    return unwrapEnvelope(await api.del(`/api/v1/Units/DeleteUnit${qs({ id })}`))
  },
  async addLesson({ title, content, unitId } = {}) {
    const form = new FormData()
    form.append('Title', title)
    form.append('Content', content)
    form.append('UnitId', unitId)
    return unwrapEnvelope(await api.upload('/api/v1/Lessons/AddLesson', form))
  },
  async updateLesson(id, { title, content, unitId } = {}) {
    const form = new FormData()
    form.append('Id', id)
    form.append('Title', title)
    form.append('Content', content)
    form.append('UnitId', unitId)
    return unwrapEnvelope(await apiJson('/api/v1/Lessons/UpdateLesson', { method: 'PUT', body: form }))
  },
  async deleteLesson(id) {
    return unwrapEnvelope(await api.del(`/api/v1/Lessons/DeleteLesson${qs({ id })}`))
  },
  // Multipart file upload — mirrors filesApi.upload's FormData pattern (durable file + metadata).
  async uploadMaterial({ lessonId, file, title, type } = {}) {
    const form = new FormData()
    form.append('File', file)
    form.append('LessonId', lessonId)
    form.append('Title', title)
    if (type !== undefined && type !== null && type !== '') form.append('Type', type)
    return unwrapEnvelope(await api.upload('/api/v1/LessonMaterial/UploadMaterial', form))
  },
  // URL-only material (no durable file) — legacy path, still [FromForm].
  async addMaterial({ lessonId, title, url, type } = {}) {
    const form = new FormData()
    form.append('LessonId', lessonId)
    form.append('Title', title)
    form.append('Url', url)
    if (type !== undefined && type !== null && type !== '') form.append('Type', type)
    return unwrapEnvelope(await api.upload('/api/v1/LessonMaterial/AddMaterial', form))
  },
  async updateMaterial(id, { title, url, type, lessonId } = {}) {
    const form = new FormData()
    form.append('Id', id)
    if (title) form.append('Title', title)
    if (url) form.append('Url', url)
    if (type !== undefined && type !== null && type !== '') form.append('Type', type)
    if (lessonId) form.append('LessonId', lessonId)
    return unwrapEnvelope(await apiJson('/api/v1/LessonMaterial/UpdateMaterial', { method: 'PUT', body: form }))
  },
  async deleteMaterial(id) {
    return unwrapEnvelope(await api.del(`/api/v1/LessonMaterial/DeleteMaterial${qs({ id })}`))
  },
}
