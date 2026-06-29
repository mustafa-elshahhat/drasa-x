// =============================================================================
// Auth API surface mapped to the real DerasaX-backend account contract
// (Phase 2 AUTHENTICATION_FLOW / AccountController). Canonical routes:
//   POST /api/v1/account/login          { UserID, Password }   -> AuthModel
//   POST /api/v1/account/refresh        (refresh cookie)        -> AuthModel
//   POST /api/v1/account/logout                                 -> { message }
//   POST /api/v1/account/change-password { CurrentPassword, NewPassword }
//
// AuthModel = { id, userName, fullName, role, token, expiresOn, isAuthenticated }
// The refresh token is an HttpOnly cookie the browser cannot read; we send it
// implicitly via credentials:'include'. Identity/role/tenant are derived by the
// backend from the signed token — the browser never asserts them.
// =============================================================================
import { config } from '../../config/env'
import { apiJson, refreshOnce, setAccessToken } from '../../lib/api/client'
import { problemFromResponse, networkError } from '../../lib/api/problemDetails'

const ACCOUNT = '/api/v1/account'

function correlationHeaders() {
  let id
  try {
    id = crypto.randomUUID()
  } catch {
    id = `cid-${Date.now()}`
  }
  return { 'X-Correlation-Id': id }
}

/** Normalize the backend AuthModel into the app's session shape. */
export function toSession(model) {
  return {
    user: { id: model.id, userName: model.userName, fullName: model.fullName || model.userName },
    role: model.role || null,
    token: model.token || null,
    expiresOn: model.expiresOn || null,
  }
}

/**
 * Login is anonymous (no bearer) and must set the refresh cookie, so it uses a
 * direct fetch with credentials rather than the authenticated client.
 * @returns {Promise<{ ok: boolean, session?: object, error?: import('../../lib/api/problemDetails').ApiError }>}
 */
export async function login(userId, password) {
  let res
  try {
    res = await fetch(`${config.backendUrl}${ACCOUNT}/login`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json', ...correlationHeaders() },
      body: JSON.stringify({ UserID: userId, Password: password }),
    })
  } catch (cause) {
    return { ok: false, error: networkError(cause) }
  }
  if (!res.ok) {
    return { ok: false, error: await problemFromResponse(res) }
  }
  const model = await res.json()
  setAccessToken(model.token)
  return { ok: true, session: toSession(model) }
}

/** Silent session restoration via the refresh cookie. Returns a session or null. */
export async function restore() {
  const model = await refreshOnce()
  if (!model?.token) return null
  return toSession(model)
}

/** Logout — best-effort backend revoke; local state cleared by the caller. */
export async function logout() {
  try {
    await fetch(`${config.backendUrl}${ACCOUNT}/logout`, {
      method: 'POST',
      credentials: 'include',
      headers: correlationHeaders(),
    })
  } catch {
    // Ignore network failures; the session is cleared locally regardless.
  } finally {
    setAccessToken(null)
  }
}

/** Change the current user's password (authenticated). Throws ApiError on failure. */
export async function changePassword({ currentPassword, newPassword }) {
  return apiJson(`${ACCOUNT}/change-password`, {
    method: 'POST',
    body: JSON.stringify({ CurrentPassword: currentPassword, NewPassword: newPassword }),
  })
}
