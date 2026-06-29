// =============================================================================
// Phase 13 — communication API surface (notifications, preferences, messaging).
// Backend-only (the canonical client talks ONLY to DerasaX-backend). Reuses the
// envelope unwrap helpers so both bare-array (notifications inbox) and ApiResponse
// envelope (preferences / conversations) shapes are handled uniformly.
// =============================================================================
import { api } from '../../lib/api/client'
import { toItems, toObject } from '../student/studentSchemas'

export const notificationsApi = {
  // ---- In-app notifications inbox ----
  async list(signal) {
    return toItems(await api.get('/api/v1/notifications?pageSize=50', { signal }))
  },
  async unreadCount(signal) {
    const r = await api.get('/api/v1/notifications/unread-count', { signal })
    return Number(r?.unreadCount ?? 0)
  },
  async markRead(id) {
    return api.patch(`/api/v1/notifications/${encodeURIComponent(id)}/read`)
  },
  async markAllRead() {
    return api.patch('/api/v1/notifications/read-all')
  },

  // ---- Notification preferences (per-user, per-category) ----
  async preferences(signal) {
    return toItems(await api.get('/api/v1/notification-preferences', { signal }))
  },
  async updatePreference({ category, inAppEnabled, emailEnabled }) {
    return toObject(await api.put('/api/v1/notification-preferences', { category, inAppEnabled, emailEnabled }))
  },

  // ---- Messaging (conversation threads + messages; participant-scoped server-side) ----
  async conversations(signal) {
    return toItems(await api.get('/api/v1/conversations', { signal }))
  },
  async startConversation({ participantUserId, studentId, subject, firstMessage }) {
    return toObject(await api.post('/api/v1/conversations', { participantUserId, studentId, subject, firstMessage }))
  },
  async conversation(id, signal) {
    return toObject(await api.get(`/api/v1/conversations/${encodeURIComponent(id)}`, { signal }))
  },
  async messages(id, signal) {
    return toItems(await api.get(`/api/v1/conversations/${encodeURIComponent(id)}/messages?pageSize=100`, { signal }))
  },
  async postMessage(id, body) {
    return toObject(await api.post(`/api/v1/conversations/${encodeURIComponent(id)}/messages`, { body }))
  },
  async markMessageRead(conversationId, messageId) {
    return api.post(
      `/api/v1/conversations/${encodeURIComponent(conversationId)}/messages/${encodeURIComponent(messageId)}/read`,
    )
  },
}
