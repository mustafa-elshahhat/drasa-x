import { describe, it, expect, vi, beforeEach } from 'vitest'

// Mock the canonical backend client and capture every call.
const calls = []
vi.mock('../../lib/api/client', () => ({
  api: {
    get: (p) => { calls.push(['GET', p]); return Promise.resolve({ data: [] }) },
    post: (p, b) => { calls.push(['POST', p, b]); return Promise.resolve({ data: { id: 'x' } }) },
  },
}))

import { visionApi } from './visionApi'

beforeEach(() => { calls.length = 0 })

describe('visionApi — backend-only client', () => {
  it('routes every call through DerasaX-backend paths (never the AI service directly)', async () => {
    await visionApi.startSession({ title: 't' })
    await visionApi.listSessions({})
    await visionApi.getSession('s1')
    await visionApi.analyzeFrame('s1', { imageBase64: 'data:image/png;base64,AAAA' })
    await visionApi.listCandidates('s1')
    await visionApi.getSummary('s1')
    await visionApi.confirmCandidate('c1', { studentId: 'stu' })
    await visionApi.rejectCandidate('c1', {})
    await visionApi.overrideCandidate('c1', { studentId: 'stu', status: 'Late' })
    await visionApi.enrollFace({ studentId: 'stu', externalLabelId: 'ext' })
    await visionApi.myEngagementSummary()
    await visionApi.childEngagementSummary('c1')

    expect(calls).toContainEqual(['POST', '/api/v1/vision/sessions', { title: 't' }])
    expect(calls).toContainEqual(['POST', '/api/v1/vision/sessions/s1/analyze', { imageBase64: 'data:image/png;base64,AAAA' }])
    expect(calls).toContainEqual(['POST', '/api/v1/vision/candidates/c1/confirm', { studentId: 'stu' }])
    expect(calls).toContainEqual(['POST', '/api/v1/vision/candidates/c1/override', { studentId: 'stu', status: 'Late' }])
    expect(calls).toContainEqual(['POST', '/api/v1/vision/enrollments', { studentId: 'stu', externalLabelId: 'ext' }])
    expect(calls).toContainEqual(['GET', '/api/v1/student/vision/engagement-summary'])
    expect(calls).toContainEqual(['GET', '/api/v1/parent/vision/children/c1/engagement-summary'])

    // Critical contract: NO absolute URL, every path is backend-relative.
    expect(calls.length).toBeGreaterThanOrEqual(12)
    expect(calls.every(([, p]) => typeof p === 'string' && p.startsWith('/api/'))).toBe(true)
    expect(calls.some(([, p]) => /^https?:\/\//i.test(p))).toBe(false)
  })

  it('fileToBase64 reads a File into a data URL', async () => {
    const { fileToBase64 } = await import('./visionApi')
    const out = await fileToBase64(new File(['hello'], 'f.png', { type: 'image/png' }))
    expect(out).toMatch(/^data:image\/png;base64,/)
  })
})
