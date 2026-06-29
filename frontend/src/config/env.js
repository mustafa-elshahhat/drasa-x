// =============================================================================
// Centralized, validated frontend configuration (Phase 7 §2).
//
//   * The ONLY place in the app that reads `import.meta.env`. Every other module
//     imports the typed `config` object from here. (Enforced by review + the
//     `no-direct-env` unit test which greps src for `import.meta.env`.)
//   * Required variables are validated at module load with zod; a missing/invalid
//     value fails LOUDLY instead of silently shipping a broken build.
//   * Only the DerasaX-backend base URL is exposed to the browser. The AI-service
//     URL is intentionally NOT a frontend variable — the browser must never hold
//     it (Phase 3/6 §18). No secrets are ever placed in VITE_* variables.
// =============================================================================
import { z } from 'zod'

// Vite only exposes variables prefixed with VITE_ to client code. We read the
// raw map exactly once here.
const raw = import.meta.env

const isBackendUrl = (val) => {
  try {
    const u = new URL(val)
    return u.protocol === 'http:' || u.protocol === 'https:'
  } catch {
    return false
  }
}

const schema = z.object({
  // The .NET system of record. The single backend origin the SPA may call.
  VITE_DOTNET_URL: z
    .string()
    .min(1, 'VITE_DOTNET_URL is required (DerasaX-backend base URL)')
    .refine(isBackendUrl, 'VITE_DOTNET_URL must be an absolute http(s) URL'),

  // Optional telemetry sink (DSN/endpoint). When absent, telemetry stays in a
  // safe console/no-op mode (Phase 7 §18). Never a secret-bearing value.
  VITE_TELEMETRY_DSN: z.string().optional().default(''),

  // Optional explicit environment name; falls back to Vite's MODE.
  VITE_APP_ENV: z.enum(['local', 'test', 'staging', 'production']).optional(),
})

function buildConfig() {
  const parsed = schema.safeParse(raw)

  if (!parsed.success) {
    const issues = parsed.error.issues.map((i) => `  - ${i.path.join('.')}: ${i.message}`).join('\n')
    const message = `Invalid frontend configuration:\n${issues}`
    // Fail clearly. In production this prevents booting a misconfigured app.
    if (raw.PROD) throw new Error(message)
    // In dev/test, warn loudly but allow a localhost fallback so the dev server
    // still boots while the developer fixes their .env.
    console.error(`[config] ${message}`)
    return null
  }

  const env = parsed.data
  const appEnv = env.VITE_APP_ENV || (raw.PROD ? 'production' : 'local')

  return Object.freeze({
    backendUrl: env.VITE_DOTNET_URL.replace(/\/+$/, ''),
    telemetryDsn: env.VITE_TELEMETRY_DSN || '',
    appEnv,
    isProduction: raw.PROD === true,
    isDev: raw.DEV === true,
    isTest: raw.MODE === 'test' || raw.MODE === 'vitest',
    mode: raw.MODE,
  })
}

const resolved = buildConfig()

// Dev/test fallback (never reached in production because buildConfig throws there).
export const config =
  resolved ||
  Object.freeze({
    backendUrl: 'http://localhost:5155',
    telemetryDsn: '',
    appEnv: 'local',
    isProduction: false,
    isDev: true,
    isTest: raw.MODE === 'test',
    mode: raw.MODE,
  })

// Exported for unit testing the validation rules in isolation.
export { schema as envSchema, buildConfig as __buildConfigForTest }
