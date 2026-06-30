// =============================================================================
// Phase 9 Teacher Portal live-acceptance support. Real browser → vite preview →
// DerasaX-backend → PostgreSQL → school-ai-rag. No network mocking. Extends the
// proven Phase 8 helpers (login/nav/guards/reset) with teacher-specific actors
// and API-driven setup (e.g. a student submission the teacher then grades).
// =============================================================================
import { request as pwRequest } from '@playwright/test'
import { BACKEND, PASSWORD, apiLogin } from './ph8.js'

export { BACKEND, PASSWORD, RESET_KEY, HAS_CREDS, backendUp, resetE2E, apiLogin, login, nav, setLang, attachGuards, axeSeriousOrCritical } from './ph8.js'

export const CODES9 = {
  teacher: 'TEACH-T1',
  teacherUnassigned: 'TEACH-T9-UNASSIGNED',
  teacherT2: 'TEACH-T2',
  studentT1: 'STU-T1',
  schoolAdmin: 'ADMIN-T1',
  parent: 'PARENT-T1',
  systemAdmin: 'SYS-1',
}

export const FIX9 = {
  classT1: 'PH8-CLASS-T1',
  classT2: 'PH8-CLASS-T2', // cross-tenant class id (teacher-1 must never see it)
  subjectT1: 'PH8-SUBJECT-T1',
  draftQuiz: 'E2E-PH9-DRAFT-T1',
  publishedQuiz: 'E2E-PH8-QUIZ-T1', // assigned to PH8-CLASS-T1, has an essay (manual grade)
  draftQuestion1: 'E2E-PH9-DRAFT-Q1',
}

// Ingest a curriculum document through the REAL teacher → backend → school-ai-rag
// flow so a live AI quiz draft is grounded (in-scope). The subject MUST match the
// subject name the backend sends (subject.Name = "Phase 8 Mathematics"). Idempotent
// server-side. Tenant is derived from the teacher token; the browser never calls AI.
export async function ensurePh9CurriculumDoc() {
  const token = await apiLogin(CODES9.teacher)
  const ctx = await pwRequest.newContext()
  try {
    const res = await ctx.post(`${BACKEND}/api/v1/ai/documents`, {
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      data: {
        documentId: 'E2E-PH9-DOC-T1', version: 1, title: 'Linear Equations Reference',
        materialType: 'textbook', language: 'en', grade: 7, subject: 'Mathematics',
        content:
          'A linear equation is an algebraic equation in which each term is a constant or the ' +
          'product of a constant and a single variable. The graph of a linear equation is a ' +
          'straight line. To solve a linear equation such as 2x + 3 = 7, subtract 3 from both ' +
          'sides to get 2x = 4, then divide by 2 to get x = 2. Linear equations have exactly one solution.',
      },
      timeout: 120000,
    })
    if (!res.ok()) throw new Error(`ensurePh9CurriculumDoc failed: ${res.status()} ${await res.text()}`)
    return await res.json()
  } finally {
    await ctx.dispose()
  }
}

// Drive the real student attempt contract to produce a SUBMITTED attempt that the
// teacher then grades. Returns the attempt/submission id. The quiz must already be
// published+assigned to the student's class (E2E-PH8-QUIZ-T1 is, for STU-T1).
export async function studentSubmitQuiz(quizId = FIX9.publishedQuiz) {
  const token = await apiLogin(CODES9.studentT1)
  const ctx = await pwRequest.newContext()
  try {
    const auth = { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` }
    const startRes = await ctx.post(`${BACKEND}/api/v1/quizzes/${quizId}/attempts`, { headers: auth, timeout: 15000 })
    if (!startRes.ok()) throw new Error(`start attempt failed: ${startRes.status()} ${await startRes.text()}`)
    const attempt = await startRes.json()
    const attemptId = attempt.id || attempt.Id || attempt?.data?.id
    if (!attemptId) throw new Error('no attempt id in start response')

    // Answer the three seeded questions (MCQ, TrueFalse, Essay).
    const answers = [
      { questionId: 'E2E-PH8-Q1', selectedOptionId: 'E2E-PH8-Q1-B' },
      { questionId: 'E2E-PH8-Q2', selectedOptionId: 'E2E-PH8-Q2-T' },
      { questionId: 'E2E-PH8-Q3', answerText: 'Subtract 1 from both sides to get x = 2.' },
    ]
    await ctx.put(`${BACKEND}/api/v1/attempts/${attemptId}/answers`, { headers: auth, data: { answers }, timeout: 15000 })
    const submitRes = await ctx.post(`${BACKEND}/api/v1/attempts/${attemptId}/submit`, { headers: auth, timeout: 15000 })
    if (!submitRes.ok()) throw new Error(`submit failed: ${submitRes.status()} ${await submitRes.text()}`)
    return attemptId
  } finally {
    await ctx.dispose()
  }
}
