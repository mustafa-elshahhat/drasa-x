// =============================================================================
// THE single, canonical API client for DerasaX-backend (Phase 7 §3).
//
// Security & contract rules enforced here:
//   * Talks ONLY to DerasaX-backend (config.backendUrl). Absolute URLs are
//     rejected, so the browser can never call school-ai-rag or any other origin.
//   * Access token lives in MEMORY only (never localStorage). The refresh token
//     is an HttpOnly cookie scoped to /api/v1/account that JS cannot read.
//   * One shared (single-flight) refresh serves all concurrent 401s, then the
//     original request is retried exactly once. A retried request is never
//     retried again — no refresh loops.
//   * Every request carries an `X-Correlation-Id` so failures can be traced
//     end-to-end; the id is surfaced on normalized errors for support.
//   * AbortSignal + a default timeout are always applied.
//   * Errors are normalized to ApiError (RFC 9457 aware). Nothing here renders
//     UI — callers/React Query decide how to present failures.
//
// This is the ONLY API client in the app. All HTTP access goes through this
// module (and the thin per-feature wrappers under features/*/api that import it);
// do not add a second client.
// =============================================================================
import { config } from '../../config/env'
import { ApiError, problemFromResponse, networkError } from './problemDetails'

const DEFAULT_TIMEOUT_MS = 20000
const ACCOUNT_BASE = '/api/v1/account'

let accessToken = null
let onAuthLost = null
let refreshPromise = null // single-flight refresh shared across concurrent 401s

export function setAccessToken(token) {
  accessToken = token || null
}
export function getAccessToken() {
  return accessToken
}
export function setOnAuthLost(fn) {
  onAuthLost = fn
}

// Test-only: reset module-level state so unit tests don't leak the single-flight
// refresh promise or token between cases.
export function __resetClientState() {
  accessToken = null
  onAuthLost = null
  refreshPromise = null
}

function newCorrelationId() {
  try {
    return crypto.randomUUID()
  } catch {
    return `cid-${Date.now()}-${Math.random().toString(16).slice(2)}`
  }
}

/** Perform exactly one refresh, shared across all concurrent callers. */
export function refreshOnce() {
  if (!refreshPromise) {
    refreshPromise = fetch(`${config.backendUrl}${ACCOUNT_BASE}/refresh`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'X-Correlation-Id': newCorrelationId() },
    })
      .then(async (res) => {
        if (!res.ok) return null
        const data = await res.json().catch(() => null)
        if (data?.token) setAccessToken(data.token)
        return data
      })
      .catch(() => null)
      .finally(() => {
        // Release on the next tick so a fresh 401 wave starts a new refresh.
        setTimeout(() => {
          refreshPromise = null
        }, 0)
      })
  }
  return refreshPromise
}

function buildSignal(options, correlationId) {
  // Honor a caller-supplied AbortSignal; otherwise apply a default timeout.
  if (options.signal) return { signal: options.signal, cleanup: () => {} }
  const controller = new AbortController()
  const ms = options.timeoutMs ?? DEFAULT_TIMEOUT_MS
  const timer = setTimeout(() => controller.abort(), ms)
  return { signal: controller.signal, cleanup: () => clearTimeout(timer), correlationId }
}

/**
 * Low-level authenticated fetch against DerasaX-backend. Returns the raw
 * Response. Throws ApiError only on network/timeout failures (HTTP errors are
 * returned so callers can branch on status; use apiJson for normalized throws).
 *
 * @param {string} path  must be a backend-relative path, e.g. "/api/v1/grades"
 * @param {RequestInit & {timeoutMs?: number, _retried?: boolean}} options
 */
export async function apiFetch(path, options = {}) {
  if (/^https?:\/\//i.test(path)) {
    throw new ApiError({
      status: 0,
      title: 'Blocked request',
      detail: 'Absolute URLs are not allowed; the client is backend-only.',
      errorCode: 'CLIENT_ABSOLUTE_URL_BLOCKED',
    })
  }

  const correlationId = options.correlationId || newCorrelationId()
  const { signal, cleanup } = buildSignal(options, correlationId)

  const doRequest = () => {
    const headers = new Headers(options.headers || {})
    headers.set('X-Correlation-Id', correlationId)
    if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`)
    // Only default JSON content-type for non-FormData bodies.
    const isFormData = typeof FormData !== 'undefined' && options.body instanceof FormData
    if (options.body && !isFormData && !headers.has('Content-Type')) {
      headers.set('Content-Type', 'application/json')
    }
    if (isFormData) headers.delete('Content-Type') // let the browser set the boundary
    return fetch(`${config.backendUrl}${path}`, {
      ...options,
      headers,
      credentials: 'include',
      signal,
    })
  }

  try {
    let res = await doRequest()

    if (res.status === 401 && !options._retried) {
      const refreshed = await refreshOnce()
      if (refreshed) {
        // The 401 was rejected at the auth layer before any side effect, so a
        // single retry is safe for any method. Mark it so it can never loop.
        res = await apiFetch(path, { ...options, _retried: true, correlationId })
      } else if (onAuthLost) {
        onAuthLost()
      }
    }
    return res
  } catch (cause) {
    throw networkError(cause, correlationId)
  } finally {
    cleanup()
  }
}

/**
 * Authenticated request returning parsed JSON. Throws a normalized ApiError on
 * any non-2xx response (RFC 9457 aware) or network/timeout failure.
 */
export async function apiJson(path, options = {}) {
  const res = await apiFetch(path, options)
  if (!res.ok) {
    throw await problemFromResponse(res)
  }
  if (res.status === 204) return null
  const contentType = res.headers.get('content-type') || ''
  if (!contentType.includes('json')) return null
  return res.json().catch(() => null)
}

/** Convenience helpers. */
export const api = {
  get: (path, options) => apiJson(path, { ...options, method: 'GET' }),
  post: (path, body, options) =>
    apiJson(path, { ...options, method: 'POST', body: body == null ? undefined : JSON.stringify(body) }),
  put: (path, body, options) =>
    apiJson(path, { ...options, method: 'PUT', body: body == null ? undefined : JSON.stringify(body) }),
  patch: (path, body, options) =>
    apiJson(path, { ...options, method: 'PATCH', body: body == null ? undefined : JSON.stringify(body) }),
  del: (path, options) => apiJson(path, { ...options, method: 'DELETE' }),
  // Multipart upload: pass a FormData instance; content-type/boundary is automatic.
  upload: (path, formData, options) => apiJson(path, { ...options, method: 'POST', body: formData }),
}

export const BACKEND = config.backendUrl
export { ApiError }
