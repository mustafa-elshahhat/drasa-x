import { describe, expect, it, vi, beforeEach } from 'vitest'
import { parentApi } from './parentApi'
import { api } from '../../lib/api/client'

vi.mock('../../lib/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}))

beforeEach(() => vi.clearAllMocks())

describe('parentApi — Phase 14 (closure) read-only child gamification', () => {
  it('reads a linked child points summary from the relationship-authorized student endpoint', async () => {
    api.get.mockResolvedValue({ data: { studentId: 'child1', totalPoints: 30 } })
    const summary = await parentApi.childPoints('child1')
    expect(summary.totalPoints).toBe(30)
    expect(api.get).toHaveBeenCalledWith('/api/v1/students/child1/points', expect.anything())
  })

  it('reads the child point ledger (paged) and returns the items array', async () => {
    api.get.mockResolvedValue({ data: [{ id: 't1', points: 10 }], totalCount: 1 })
    const ledger = await parentApi.childPointsLedger('child1')
    expect(ledger).toHaveLength(1)
    expect(api.get).toHaveBeenCalledWith('/api/v1/students/child1/points/ledger?pageSize=50', expect.anything())
  })

  it('exposes no award/mutation surface to parents and only targets backend-relative paths', async () => {
    api.get.mockResolvedValue({ data: {} })
    await parentApi.childPoints('child1')
    await parentApi.childPointsLedger('child1')
    expect(parentApi.awardPoints).toBeUndefined()
    // Parent gamification reads are GET-only.
    expect(api.post).not.toHaveBeenCalled()
    expect(api.put).not.toHaveBeenCalled()
    for (const [path] of api.get.mock.calls) {
      expect(path.startsWith('/api/v1/')).toBe(true)
      expect(/^https?:\/\//i.test(path)).toBe(false)
    }
  })
})
