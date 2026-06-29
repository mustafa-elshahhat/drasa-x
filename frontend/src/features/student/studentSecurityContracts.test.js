import { beforeEach, describe, expect, it, vi } from 'vitest'

// Phase 8 §11 — direct behavioural coverage of SECURITY-SENSITIVE request
// construction. The browser must never send authority-owned values (tenant,
// student/author identity, score, correct-answer decision, rank, points). The
// backend derives all of these from the trusted auth context. These tests
// assert the EXACT payloads the student API layer puts on the wire.

vi.mock('../../lib/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}))

import { api } from '../../lib/api/client'
import { studentApi } from './studentApi'
import { answerPayloadFromForm } from './studentUtils'

const IDENTITY_KEYS = ['tenantId', 'TenantId', 'studentId', 'StudentId', 'authorId', 'AuthorId', 'userId', 'UserId']
const AUTHORITY_KEYS = ['score', 'Score', 'correctOptionId', 'CorrectOptionId', 'isCorrect', 'rank', 'Rank', 'points', 'Points']

function bodyOf(call) {
  return call?.[1]?.body !== undefined ? call[1].body : call?.[1]
}

describe('student security contracts — no authority-owned fields leave the browser', () => {
  beforeEach(() => vi.clearAllMocks())

  it('anonymous suggestion payload carries only title + body (no tenant/student/author id)', async () => {
    api.post.mockResolvedValue({ data: { id: 's1' } })
    await studentApi.submitSuggestion('Better lab time', 'Please move the lab earlier.')
    const [url, opts] = api.post.mock.calls[0]
    expect(url).toBe('/api/v1/suggestions')
    expect(opts).toEqual({ title: 'Better lab time', body: 'Please move the lab earlier.' })
    for (const k of IDENTITY_KEYS) expect(opts).not.toHaveProperty(k)
  })

  it('quiz answer payload from a form is limited to {questionId, selectedOptionId}', () => {
    const form = new FormData()
    form.set('q:question-1', 'option-A')
    form.set('q:question-2', 'option-B')
    form.set('score', '100') // hostile extra field must be ignored
    form.set('rank', '1')
    const answers = answerPayloadFromForm(form)
    expect(answers).toEqual([
      { questionId: 'question-1', selectedOptionId: 'option-A' },
      { questionId: 'question-2', selectedOptionId: 'option-B' },
    ])
    for (const a of answers) {
      expect(Object.keys(a).sort()).toEqual(['questionId', 'selectedOptionId'])
      for (const k of AUTHORITY_KEYS) expect(a).not.toHaveProperty(k)
    }
  })

  it('saveAttempt forwards only the answers array (no score/correct/rank)', async () => {
    api.put.mockResolvedValue({ data: {} })
    const answers = [{ questionId: 'q1', selectedOptionId: 'o1' }]
    await studentApi.saveAttempt('attempt-1', answers)
    const [url, opts] = api.put.mock.calls[0]
    expect(url).toBe('/api/v1/attempts/attempt-1/answers')
    expect(opts).toEqual({ answers })
    const serialized = JSON.stringify(opts)
    for (const k of AUTHORITY_KEYS) expect(serialized).not.toContain(`"${k}"`)
  })

  it('submitAttempt sends no client-side body at all', async () => {
    api.post.mockResolvedValue({ data: { id: 'r1' } })
    await studentApi.submitAttempt('attempt-1')
    expect(api.post).toHaveBeenCalledWith('/api/v1/attempts/attempt-1/submit')
    expect(bodyOf(api.post.mock.calls[0])).toBeUndefined()
  })

  it('entering a competition sends no client-supplied points/score/rank', async () => {
    api.post.mockResolvedValue({ data: { id: 'e1' } })
    await studentApi.enterCompetition('comp-1')
    expect(api.post).toHaveBeenCalledWith('/api/v1/competitions/comp-1/entries')
    expect(bodyOf(api.post.mock.calls[0])).toBeUndefined()
  })

  it('booking an office hour forwards only the free-text note', async () => {
    api.post.mockResolvedValue({ data: { id: 'b1' } })
    await studentApi.bookOfficeHour('session-1', 'Need help with algebra')
    const [url, opts] = api.post.mock.calls[0]
    expect(url).toBe('/api/v1/office-hours/session-1/bookings')
    expect(opts).toEqual({ notes: 'Need help with algebra' })
    for (const k of IDENTITY_KEYS) expect(opts).not.toHaveProperty(k)
  })
})
