// =============================================================================
// Auth API surface mapped to the real DerasaX-backend account contract
// (Phase 2 AUTHENTICATION_FLOW / AccountController). Canonical routes:
//   POST /api/v1/account/login          { UserID, Password }   -> AuthModel
//   POST /api/v1/account/refresh        (refresh cookie)        -> AuthModel
//   POST /api/v1/account/logout                                 -> { message }
//   POST /api/v1/account/change-password { CurrentPassword, NewPassword }
//
// AuthModel = { id, userName, fullName, role, token, expiresOn, isAuthenticated, mustChangePassword }
// mustChangePassword: true means the account signed in with a temporary/reset password and
// must change it before using any other endpoint (see AuthContext + guards.jsx).
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
    mustChangePassword: Boolean(model.mustChangePassword),
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

/**
 * Forgot-password is anonymous and ALWAYS returns 200 with the same generic shape —
 * the backend never discloses whether the login code belongs to a real account.
 * Uses a direct fetch (same pattern as `login`) since there is no session yet.
 * `devToken` is populated only in Development (no email provider configured).
 * @returns {Promise<{ ok: boolean, message?: string, devToken?: string|null, error?: import('../../lib/api/problemDetails').ApiError }>}
 */
export async function forgotPassword(loginCode) {
  let res
  try {
    res = await fetch(`${config.backendUrl}${ACCOUNT}/forgot-password`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json', ...correlationHeaders() },
      body: JSON.stringify({ LoginCode: loginCode }),
    })
  } catch (cause) {
    return { ok: false, error: networkError(cause) }
  }
  if (!res.ok) {
    return { ok: false, error: await problemFromResponse(res) }
  }
  const body = await res.json()
  return { ok: true, message: body.message, devToken: body.devToken || null }
}

/**
 * Reset-password is anonymous. Fails with a 400 ("Invalid or expired reset token.")
 * when the token is wrong/expired/already used. Uses a direct fetch (same pattern
 * as `login`) since there is no session yet.
 * @returns {Promise<{ ok: boolean, message?: string, error?: import('../../lib/api/problemDetails').ApiError }>}
 */
export async function resetPassword({ loginCode, token, newPassword }) {
  let res
  try {
    res = await fetch(`${config.backendUrl}${ACCOUNT}/reset-password`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json', ...correlationHeaders() },
      body: JSON.stringify({ LoginCode: loginCode, Token: token, NewPassword: newPassword }),
    })
  } catch (cause) {
    return { ok: false, error: networkError(cause) }
  }
  if (!res.ok) {
    return { ok: false, error: await problemFromResponse(res) }
  }
  const body = await res.json()
  return { ok: true, message: body.message }
}
