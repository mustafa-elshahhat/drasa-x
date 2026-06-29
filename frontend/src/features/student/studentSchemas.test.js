import { describe, expect, it } from 'vitest'
import { normalizeAttendance, normalizeTutorResponse, toItems, toObject, unwrapEnvelope } from './studentSchemas'

describe('student response schemas', () => {
  it('unwraps DerasaX ApiResponse envelopes without inventing fallback data', () => {
    expect(unwrapEnvelope({ statusCode: 200, data: [{ id: 's1' }] })).toEqual([{ id: 's1' }])
    expect(toItems({ data: { items: [{ id: 'a' }], totalCount: 1 } })).toEqual([{ id: 'a' }])
    expect(toItems({ data: null })).toEqual([])
  })

  it('normalizes object payloads only when the server returned an object', () => {
    expect(toObject({ data: { id: 'x' } })).toEqual({ id: 'x' })
    expect(toObject({ data: [] })).toBeNull()
  })

  it('normalizes safe structured AI tutor citations', () => {
    const result = normalizeTutorResponse({ Answer: 'Use the theorem.', Grounded: true, Citations: [{ title: 'Math' }], CorrelationId: 'cid-1' })
    expect(result.answer).toBe('Use the theorem.')
    expect(result.grounded).toBe(true)
    expect(result.citations).toEqual([{ title: 'Math' }])
    expect(result.correlationId).toBe('cid-1')
  })

  it('normalizes attendance records and backend-owned summary without fabricated rows', () => {
    const result = normalizeAttendance({
      data: {
        summary: { total: 2, present: 1, absent: 1, late: 0, excused: 0, attendancePercentage: 50 },
        records: [{ id: 'a1', attendanceDate: '2031-01-05T00:00:00Z', status: 'Present', source: 'Manual', sessionKey: 'day' }],
      },
    })
    expect(result.summary.attendancePercentage).toBe(50)
    expect(result.records).toEqual([{ id: 'a1', attendanceDate: '2031-01-05T00:00:00Z', status: 'Present', recordedAt: '', source: 'Manual', sessionKey: 'day', schoolClassId: null, notes: null }])
  })

  it('rejects invalid attendance payloads instead of inventing fallback percentages', () => {
    expect(() => normalizeAttendance({ data: { summary: {}, records: 'bad' } })).toThrow()
  })
})
