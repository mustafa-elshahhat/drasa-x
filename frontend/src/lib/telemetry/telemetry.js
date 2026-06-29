// =============================================================================
// Frontend telemetry / diagnostics abstraction (Phase 7 §18).
//
//   * Captures unexpected UI errors and route-level failures.
//   * Includes a correlation ID when one is available.
//   * REDACTS sensitive material — it never forwards tokens, passwords, prompts,
//     educational content, messages, or arbitrary PII.
//   * Provider is configurable. With no DSN configured it falls back to a
//     dev-only console sink (and is silent in production), so there is no
//     mandatory third-party production dependency.
//   * Phase 19 integration point: replace `forward()` with the chosen provider's
//     SDK call. The redaction + shape contract below stays the same.
// =============================================================================
import { config } from '../../config/env'

const SENSITIVE_KEYS = [
  'token',
  'accesstoken',
  'refreshtoken',
  'password',
  'currentpassword',
  'newpassword',
  'authorization',
  'cookie',
  'secret',
  'prompt',
  'message',
  'content',
  'email',
]

/** Recursively strip sensitive values from a context object before sending. */
export function redact(value, depth = 0) {
  if (depth > 4) return '[depth-limit]'
  if (value == null) return value
  if (typeof value === 'string') {
    // Strip anything that looks like a bearer token / JWT.
    if (/^bearer\s+/i.test(value) || /eyJ[A-Za-z0-9_-]{10,}\./.test(value)) return '[redacted-token]'
    return value.length > 500 ? value.slice(0, 500) + '…' : value
  }
  if (typeof value !== 'object') return value
  if (Array.isArray(value)) return value.slice(0, 20).map((v) => redact(v, depth + 1))
  const out = {}
  for (const [k, v] of Object.entries(value)) {
    if (SENSITIVE_KEYS.includes(k.toLowerCase())) {
      out[k] = '[redacted]'
    } else {
      out[k] = redact(v, depth + 1)
    }
  }
  return out
}

function safeMessage(error) {
  if (!error) return 'unknown'
  if (typeof error === 'string') return error.slice(0, 300)
  return (error.title || error.message || 'error').toString().slice(0, 300)
}

function forward(event) {
  // No provider configured -> dev console only; silent in production.
  if (!config.telemetryDsn) {
    if (config.isDev) console.warn('[telemetry]', event.kind, event)
    return
  }
  // Phase 19: send `event` to the configured provider here. The payload is
  // already redacted and bounded.
  if (config.isDev) console.warn('[telemetry:provider]', event.kind, event)
}

export const telemetry = {
  captureError(error, context = {}) {
    forward({
      kind: 'error',
      message: safeMessage(error),
      status: error?.status ?? null,
      errorCode: error?.errorCode ?? null,
      correlationId: error?.correlationId ?? context.correlationId ?? null,
      context: redact(context),
      appEnv: config.appEnv,
    })
  },
  captureRouteError(error, routeInfo = {}) {
    forward({
      kind: 'route-error',
      message: safeMessage(error),
      route: redact(routeInfo),
      correlationId: error?.correlationId ?? null,
      appEnv: config.appEnv,
    })
  },
  captureMessage(message, context = {}) {
    forward({ kind: 'message', message: String(message).slice(0, 300), context: redact(context), appEnv: config.appEnv })
  },
}
