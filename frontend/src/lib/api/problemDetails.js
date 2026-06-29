// =============================================================================
// RFC 9457 Problem Details normalization (Phase 7 §3/§11).
//
// The DerasaX-backend emits `application/problem+json` extended with
// `errorCode`, `correlationId`, `timestamp`, `retryable` (Phase 2 ERROR_CONTRACT)
// and an `X-Correlation-Id` response header. This module turns any failed
// response — Problem Details, plain JSON, or an opaque/network failure — into one
// stable, safe-to-render shape. It never exposes stack traces or internal URLs.
// =============================================================================

/** Canonical normalized API error. */
export class ApiError extends Error {
  constructor({ status, title, detail, errorCode, correlationId, fieldErrors, retryable, type, raw }) {
    super(title || detail || `Request failed (${status ?? 'network'})`)
    this.name = 'ApiError'
    this.status = status ?? 0
    this.title = title || null
    this.detail = detail || null
    this.errorCode = errorCode || null
    this.correlationId = correlationId || null
    this.fieldErrors = fieldErrors || null // { field: [messages] }
    this.retryable = Boolean(retryable)
    this.type = type || null
    this.raw = raw || null
  }

  /** True when this is a network/timeout failure rather than an HTTP response. */
  get isNetworkError() {
    return this.status === 0
  }
}

/**
 * Build a normalized ApiError from a fetch Response (already known to be !ok).
 * @param {Response} res
 */
export async function problemFromResponse(res) {
  const headerCorrelation = res.headers.get('X-Correlation-Id') || res.headers.get('x-correlation-id')
  let body = null
  const contentType = res.headers.get('content-type') || ''
  try {
    if (contentType.includes('json')) body = await res.json()
    else {
      const text = await res.text()
      body = text ? { detail: text.slice(0, 500) } : null
    }
  } catch {
    body = null
  }

  // ASP.NET ValidationProblemDetails -> { errors: { field: [msgs] } }
  let fieldErrors = null
  if (body && body.errors && typeof body.errors === 'object' && !Array.isArray(body.errors)) {
    fieldErrors = {}
    for (const [key, val] of Object.entries(body.errors)) {
      fieldErrors[key] = Array.isArray(val) ? val : [String(val)]
    }
  }

  return new ApiError({
    status: res.status,
    title: body?.title,
    detail: body?.detail,
    errorCode: body?.errorCode,
    correlationId: body?.correlationId || headerCorrelation,
    fieldErrors,
    retryable: body?.retryable,
    type: body?.type,
    raw: body,
  })
}

/** Build a normalized ApiError for a network/timeout failure (no response). */
export function networkError(cause, correlationId) {
  const aborted = cause?.name === 'AbortError'
  return new ApiError({
    status: 0,
    title: aborted ? 'Request timed out' : 'Network error',
    detail: aborted
      ? 'The request took too long and was cancelled.'
      : 'Unable to reach the server. Check your connection and try again.',
    errorCode: aborted ? 'TIMEOUT' : 'NETWORK_ERROR',
    correlationId: correlationId || null,
    retryable: true,
  })
}

/**
 * A short, safe, user-facing message for any normalized error. Maps the common
 * HTTP statuses to friendly copy; never leaks server internals.
 */
export function userMessageFor(error, t) {
  const tr = typeof t === 'function' ? t : (_k, d) => d
  if (!(error instanceof ApiError)) return tr('errors.generic', 'Something went wrong. Please try again.')
  switch (error.status) {
    case 0:
      return error.detail || tr('errors.network', 'Network error. Please try again.')
    case 401:
      return tr('errors.unauthorized', 'Your session has expired. Please sign in again.')
    case 403:
      return tr('errors.forbidden', 'You do not have permission to perform this action.')
    case 404:
      return tr('errors.notFound', 'The requested item could not be found.')
    case 409:
      return tr('errors.conflict', 'This action conflicts with the current state. Please refresh and retry.')
    case 422:
      return error.title || tr('errors.validation', 'Please correct the highlighted fields.')
    case 429:
      return tr('errors.rateLimited', 'Too many requests. Please wait a moment and try again.')
    default:
      if (error.status >= 500) return tr('errors.server', 'A server error occurred. Please try again later.')
      return error.title || error.detail || tr('errors.generic', 'Something went wrong. Please try again.')
  }
}
