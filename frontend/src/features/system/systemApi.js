// =============================================================================
// Phase 12 — System Admin (platform) Portal data access. Uses the SINGLE canonical
// API client (DerasaX-backend only); the browser never calls school-ai-rag. Every
// endpoint is SystemAdmin-guarded server-side — the UI only renders what the backend
// returns and fabricates no statistics, ids, or records.
//
// Tenant lifecycle / plans / platform audit / system settings / feature flags REUSE
// the existing Phase 5 §14 SystemAdmin contracts (`/api/v1/tenants`, `/platform-audit`,
// `/system-settings`, `/feature-flags`). The aggregate dashboard, platform usage/AI/
// storage roll-ups, cross-tenant support inbox, platform announcements, create-initial-
// school-admin, operational status, and the SAFE non-destructive tenant data workflow
// use the NEW Phase 12 `/api/v1/system-admin/...` contracts.
//
// Enums are bound NUMERICALLY by the backend (no JsonStringEnumConverter), so write
// payloads send numeric enum values (see the *_VALUE maps in the page component).
// =============================================================================
import { api } from '../../lib/api/client'
import { toItems, toObject, unwrapEnvelope } from '../student/studentSchemas'

const q = (params) => {
  const usp = new URLSearchParams()
  Object.entries(params || {}).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') usp.append(k, v)
  })
  const s = usp.toString()
  return s ? `?${s}` : ''
}

const enc = encodeURIComponent

export const systemApi = {
  // ---- aggregate platform dashboard (Phase 12 contract) ----
  async dashboard(signal) {
    return toObject(await api.get('/api/v1/system-admin/dashboard', { signal }))
  },

  // ---- tenants lifecycle (reused Phase 5 §14 contracts) ----
  async tenants(status, signal) {
    return toItems(await api.get(`/api/v1/tenants${q({ status, pageSize: 100 })}`, { signal }))
  },
  async tenant(id, signal) {
    return toObject(await api.get(`/api/v1/tenants/${enc(id)}`, { signal }))
  },
  async tenantSubscription(id, signal) {
    return toObject(await api.get(`/api/v1/tenants/${enc(id)}/subscription`, { signal }))
  },
  async tenantUsage(id, signal) {
    return toObject(await api.get(`/api/v1/tenants/${enc(id)}/usage`, { signal }))
  },
  async createTenant(body) {
    return unwrapEnvelope(await api.post('/api/v1/tenants', body))
  },
  async setTenantStatus(id, action) {
    // action: 'activate' | 'suspend' | 'reactivate' | 'archive'
    return unwrapEnvelope(await api.post(`/api/v1/tenants/${enc(id)}/${action}`))
  },

  // ---- plans & subscriptions ----
  async plans(signal) {
    return toItems(await api.get('/api/v1/tenants/plans', { signal }))
  },
  async createPlan(body) {
    return unwrapEnvelope(await api.post('/api/v1/tenants/plans', body))
  },
  async updatePlan(id, body) {
    return unwrapEnvelope(await api.put(`/api/v1/tenants/plans/${enc(id)}`, body))
  },
  async assignPlan(body) {
    // body: { tenantId, planDefinitionId, isTrial, expiresAt? }
    return unwrapEnvelope(await api.post('/api/v1/tenants/subscriptions', body))
  },
  async subscriptions(signal) {
    return toItems(await api.get('/api/v1/system-admin/subscriptions', { signal }))
  },

  // ---- initial school admin (Phase 12 contract) ----
  async createSchoolAdmin(tenantId, body) {
    // body: { fullName, loginCode } — returns a one-time credential.
    return unwrapEnvelope(await api.post(`/api/v1/system-admin/tenants/${enc(tenantId)}/school-admins`, body))
  },

  // ---- platform usage / AI / storage roll-ups (Phase 12 contracts) ----
  async usage(signal) {
    return toObject(await api.get('/api/v1/system-admin/usage', { signal }))
  },
  async aiUsage(signal) {
    return toObject(await api.get('/api/v1/system-admin/ai-usage', { signal }))
  },
  async storage(signal) {
    return toObject(await api.get('/api/v1/system-admin/storage', { signal }))
  },

  // ---- cross-tenant support inbox (Phase 12 contract) ----
  async supportTickets(params, signal) {
    return toItems(await api.get(`/api/v1/system-admin/support-tickets${q({ ...params, pageSize: 100 })}`, { signal }))
  },
  async respondSupportTicket(id, body) {
    // body: { responseMessage, status }
    return unwrapEnvelope(await api.post(`/api/v1/system-admin/support-tickets/${enc(id)}/respond`, body))
  },

  // ---- platform announcements (Phase 12 contract) ----
  async announcements(signal) {
    return toItems(await api.get('/api/v1/system-admin/announcements', { signal }))
  },
  async createAnnouncement(body) {
    return unwrapEnvelope(await api.post('/api/v1/system-admin/announcements', body))
  },

  // ---- feature flags & platform settings (reused Phase 5 §14 contracts) ----
  async featureFlags(signal) {
    return toItems(await api.get('/api/v1/feature-flags', { signal }))
  },
  async upsertFeatureFlag(body) {
    // body: { key, isEnabled, targetTenantId? }
    return unwrapEnvelope(await api.put('/api/v1/feature-flags', body))
  },
  async settings(signal) {
    return toItems(await api.get('/api/v1/system-settings', { signal }))
  },
  async upsertSetting(body) {
    // body: { key, value, valueType, isSecret }
    return unwrapEnvelope(await api.put('/api/v1/system-settings', body))
  },

  // ---- audit & security events (reused platform-audit; security = filtered) ----
  async audit(signal) {
    return toItems(await api.get(`/api/v1/platform-audit${q({ pageSize: 100 })}`, { signal }))
  },
  async securityEvents(signal) {
    // Security events are derived from the REAL audit trail (Login action = 3). A dedicated
    // security-event/SIEM pipeline is honestly deferred (see operational-status note).
    return toItems(await api.get(`/api/v1/platform-audit${q({ action: 3, pageSize: 100 })}`, { signal }))
  },

  // ---- operational posture: real health + honest deferrals (Phase 12 contract) ----
  async operationalStatus(signal) {
    return toObject(await api.get('/api/v1/system-admin/operational-status', { signal }))
  },

  // ---- SAFE, non-destructive tenant data workflow (Phase 12 contract) ----
  async exportTenantData(id) {
    return unwrapEnvelope(await api.post(`/api/v1/system-admin/tenants/${enc(id)}/data-export`))
  },
  async requestTenantDeletion(id) {
    return unwrapEnvelope(await api.post(`/api/v1/system-admin/tenants/${enc(id)}/data-deletion-request`))
  },
}
