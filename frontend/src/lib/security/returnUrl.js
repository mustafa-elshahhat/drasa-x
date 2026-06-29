// =============================================================================
// Safe return-URL handling — open-redirect prevention (Phase 7 §19).
//
// After login we send the user back to where they came from. An attacker must
// not be able to craft `?from=https://evil.example` or `//evil.example` and have
// us navigate there. We accept ONLY same-app, single-leading-slash, relative
// paths. Everything else collapses to a safe default.
// =============================================================================

const INVALID = ' __INVALID__'

/** True if the string contains any whitespace or ASCII control char (<= 0x20 or 0x7f). */
function hasControlOrSpace(s) {
  for (let i = 0; i < s.length; i++) {
    const c = s.charCodeAt(i)
    if (c <= 0x20 || c === 0x7f) return true
  }
  return false
}

/**
 * Validate and normalize a return path.
 * @param {string|undefined|null} candidate
 * @param {string} fallback default safe path
 * @returns {string} a safe, app-relative path
 */
export function safeReturnPath(candidate, fallback = '/app') {
  if (typeof candidate !== 'string' || candidate.length === 0) return fallback

  // Reject absolute URLs (http:, https:, javascript:, data:, etc.).
  if (/^[a-z][a-z0-9+.-]*:/i.test(candidate)) return fallback

  // Reject protocol-relative ("//host") and backslash variants ("/\", "\\..").
  if (/^[/\\]{2,}/.test(candidate)) return fallback
  if (/^\/\\/.test(candidate)) return fallback
  if (/^\\/.test(candidate)) return fallback

  // Must be a single-leading-slash, app-relative path.
  if (!candidate.startsWith('/')) return fallback

  // Reject whitespace / control characters (does NOT reject hyphens).
  if (hasControlOrSpace(candidate)) return fallback

  return candidate
}

/** True when the path is a safe in-app relative path. */
export function isSafeReturnPath(candidate) {
  return safeReturnPath(candidate, INVALID) !== INVALID
}
