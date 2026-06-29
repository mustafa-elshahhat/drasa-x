import { describe, expect, it, vi, beforeEach } from 'vitest'
import { studentApi } from './studentApi'
import { api } from '../../lib/api/client'

vi.mock('../../lib/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}))

beforeEach(() => vi.clearAllMocks())

describe('studentApi — Phase 14 gamification (backend-only contract)', () => {
  it('reads the points summary from the canonical per-student endpoint', async () => {
    api.get.mockResolvedValue({ data: { studentId: 'stu1', totalPoints: 42 } })
    const summary = await studentApi.points('stu1')
    expect(summary.totalPoints).toBe(42)
    expect(api.get).toHaveBeenCalledWith('/api/v1/students/stu1/points', expect.anything())
  })

  it('reads the point ledger (paged) and returns the items array', async () => {
    api.get.mockResolvedValue({ data: [{ id: 't1', points: 10 }], totalCount: 1 })
    const ledger = await studentApi.pointsLedger('stu1')
    expect(ledger).toHaveLength(1)
    expect(api.get).toHaveBeenCalledWith('/api/v1/students/stu1/points/ledger?pageSize=50', expect.anything())
  })

  it('reads the tenant-scoped points leaderboard, with an optional grade filter', async () => {
    api.get.mockResolvedValue({ data: [{ studentId: 'stu1', totalPoints: 99, rank: 1 }] })
    await studentApi.pointsLeaderboard({})
    expect(api.get).toHaveBeenCalledWith('/api/v1/gamification/leaderboard?pageSize=50', expect.anything())
    await studentApi.pointsLeaderboard({ gradeId: 'G7' })
    expect(api.get).toHaveBeenCalledWith('/api/v1/gamification/leaderboard?gradeId=G7&pageSize=50', expect.anything())
  })

  it('submits durable competition work via POST to the submissions endpoint', async () => {
    api.post.mockResolvedValue({ data: { id: 's1', content: 'My answer' } })
    const result = await studentApi.submitCompetition('comp1', 'My answer')
    expect(result.id).toBe('s1')
    expect(api.post).toHaveBeenCalledWith('/api/v1/competitions/comp1/submissions', { content: 'My answer' })
  })

  it('reads the own competition submission, surfacing a 404 (not submitted yet) as null', async () => {
    api.get.mockResolvedValue({ data: { id: 's1', content: 'A' } })
    expect((await studentApi.competitionSubmission('comp1'))?.content).toBe('A')
    expect(api.get).toHaveBeenCalledWith('/api/v1/competitions/comp1/submissions/me', expect.anything())

    api.get.mockRejectedValueOnce({ status: 404 })
    expect(await studentApi.competitionSubmission('comp2')).toBeNull()
  })

  it('never awards points from the student client (read-only surface) and only targets backend paths', () => {
    expect(studentApi.awardPoints).toBeUndefined()
    const calls = [...api.get.mock.calls, ...api.post.mock.calls, ...api.put.mock.calls, ...api.patch.mock.calls]
    for (const [path] of calls) {
      expect(path.startsWith('/api/v1/')).toBe(true)
      expect(/^https?:\/\//i.test(path)).toBe(false)
    }
  })
})
