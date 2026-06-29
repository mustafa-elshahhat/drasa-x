import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../lib/api/client', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
  },
}))

import { api } from '../../lib/api/client'
import { studentApi } from './studentApi'

describe('studentApi Phase 8 contracts', () => {
  beforeEach(() => vi.clearAllMocks())

  it('reads attendance from the student self endpoint only', async () => {
    api.get.mockResolvedValue({ data: { summary: { total: 0 }, records: [] } })
    await expect(studentApi.attendance()).resolves.toEqual({
      summary: { total: 0, present: 0, absent: 0, late: 0, excused: 0, attendancePercentage: 0 },
      records: [],
    })
    expect(api.get).toHaveBeenCalledWith('/api/v1/student/attendance', { signal: undefined })
  })

  it('constructs lesson completion without tenant or student identity in the body', async () => {
    api.post.mockResolvedValue({ data: { id: 'p1', lessonId: 'lesson/1', isCompleted: true } })
    await studentApi.completeLesson('lesson/1')
    expect(api.post).toHaveBeenCalledWith('/api/v1/student/lessons/lesson%2F1/complete')
  })
})
