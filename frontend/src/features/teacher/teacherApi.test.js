// =============================================================================
// Phase 22 / FE-02 — the Teacher Portal data-access layer must never resolve to
// `undefined` (React Query rejects an `undefined` query result). The quizzes list
// query is the one that previously surfaced the "Query data cannot be undefined"
// warning under an unconfigured mock; this locks the contract for every backend
// envelope shape (raw array, {items}, {data}, null data, bare object, undefined).
// =============================================================================
import { describe, it, expect, vi, beforeEach } from 'vitest'

vi.mock('../../lib/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn(), del: vi.fn() },
}))

import { api } from '../../lib/api/client'
import { teacherApi } from './teacherApi'

const LIST_SHAPES = [
  undefined,
  null,
  [],
  [{ quizId: 'q1' }],
  { items: [{ quizId: 'q1' }] },
  { data: [{ quizId: 'q1' }] },
  { data: null },
  { succeeded: true, data: [] },
]

const OBJECT_SHAPES = [undefined, null, {}, { studentCount: 3 }, { data: { studentCount: 3 } }, { data: null }]

describe('teacherApi — query functions never resolve to undefined (FE-02)', () => {
  beforeEach(() => vi.clearAllMocks())

  it('quizzes() returns an array for every backend shape, never undefined', async () => {
    for (const shape of LIST_SHAPES) {
      api.get.mockResolvedValueOnce(shape)
      const result = await teacherApi.quizzes()
      expect(result).not.toBeUndefined()
      expect(Array.isArray(result)).toBe(true)
    }
  })

  it('dashboard() returns an object or null for every backend shape, never undefined', async () => {
    for (const shape of OBJECT_SHAPES) {
      api.get.mockResolvedValueOnce(shape)
      const result = await teacherApi.dashboard()
      expect(result).not.toBeUndefined()
      expect(result === null || typeof result === 'object').toBe(true)
    }
  })
})
