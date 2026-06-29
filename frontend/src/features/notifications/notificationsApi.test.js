import { describe, expect, it, vi, beforeEach } from 'vitest'
import { notificationsApi } from './notificationsApi'
import { api } from '../../lib/api/client'

vi.mock('../../lib/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}))

beforeEach(() => vi.clearAllMocks())

describe('notificationsApi — backend-only contract', () => {
  it('reads the unread count from the canonical endpoint', async () => {
    api.get.mockResolvedValue({ unreadCount: 5 })
    expect(await notificationsApi.unreadCount()).toBe(5)
    expect(api.get).toHaveBeenCalledWith('/api/v1/notifications/unread-count', expect.anything())
  })

  it('marks a single notification read via PATCH', async () => {
    api.patch.mockResolvedValue(null)
    await notificationsApi.markRead('n1')
    expect(api.patch).toHaveBeenCalledWith('/api/v1/notifications/n1/read')
  })

  it('marks all read via PATCH', async () => {
    api.patch.mockResolvedValue(null)
    await notificationsApi.markAllRead()
    expect(api.patch).toHaveBeenCalledWith('/api/v1/notifications/read-all')
  })

  it('updates a preference via PUT with the category payload', async () => {
    api.put.mockResolvedValue({ data: { category: 6, inAppEnabled: false } })
    await notificationsApi.updatePreference({ category: 6, inAppEnabled: false, emailEnabled: false })
    expect(api.put).toHaveBeenCalledWith('/api/v1/notification-preferences', {
      category: 6,
      inAppEnabled: false,
      emailEnabled: false,
    })
  })

  it('posts a message body to the conversation messages endpoint', async () => {
    api.post.mockResolvedValue({ data: { id: 'm1' } })
    await notificationsApi.postMessage('c1', 'hello')
    expect(api.post).toHaveBeenCalledWith('/api/v1/conversations/c1/messages', { body: 'hello' })
  })

  it('only ever targets backend-relative paths (never the AI service or an absolute URL)', () => {
    const calls = [...api.get.mock.calls, ...api.post.mock.calls, ...api.put.mock.calls, ...api.patch.mock.calls]
    for (const [path] of calls) {
      expect(path.startsWith('/api/v1/')).toBe(true)
      expect(/^https?:\/\//i.test(path)).toBe(false)
    }
  })
})
