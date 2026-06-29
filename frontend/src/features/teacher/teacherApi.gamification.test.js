import { describe, expect, it, vi, beforeEach } from 'vitest'
import { teacherApi } from './teacherApi'
import { api } from '../../lib/api/client'

vi.mock('../../lib/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}))

beforeEach(() => vi.clearAllMocks())

describe('teacherApi — Phase 14 gamification authoring (backend-only contract)', () => {
  it('awards points via POST with the points/reason/idempotencyKey body', async () => {
    api.post.mockResolvedValue({ data: { id: 'tx1' } })
    await teacherApi.awardPoints('stu1', { points: 25, reason: 'Great work', idempotencyKey: 'k1' })
    expect(api.post).toHaveBeenCalledWith('/api/v1/students/stu1/points', { points: 25, reason: 'Great work', idempotencyKey: 'k1' })
  })

  it('marks office-hour attendance via the booking attendance endpoint', async () => {
    api.post.mockResolvedValue({ data: { id: 'b1', status: 'Attended' } })
    await teacherApi.markAttendance('b1', 3)
    expect(api.post).toHaveBeenCalledWith('/api/v1/bookings/b1/attendance', { status: 3 })
  })

  it('lists and upserts gamification rules via the canonical endpoints', async () => {
    api.get.mockResolvedValue({ data: [{ id: 'r1', code: 'OH', points: 10 }] })
    const rules = await teacherApi.gamificationRules()
    expect(rules).toHaveLength(1)
    expect(api.get).toHaveBeenCalledWith('/api/v1/gamification/rules', expect.anything())

    api.put.mockResolvedValue({ data: { id: 'r1' } })
    await teacherApi.upsertGamificationRule({ code: 'OH', name: 'x', trigger: 0, points: 10, enabled: true })
    expect(api.put).toHaveBeenCalledWith('/api/v1/gamification/rules', { code: 'OH', name: 'x', trigger: 0, points: 10, enabled: true })
  })

  it('only ever targets backend-relative paths', () => {
    const calls = [...api.get.mock.calls, ...api.post.mock.calls, ...api.put.mock.calls, ...api.patch.mock.calls]
    for (const [path] of calls) {
      expect(path.startsWith('/api/v1/')).toBe(true)
      expect(/^https?:\/\//i.test(path)).toBe(false)
    }
  })
})
