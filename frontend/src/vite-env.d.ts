/// <reference types="vite/client" />
/// <reference types="vite-plugin-pwa/client" />

// Typed view of the VITE_* variables the app reads. NOTE: only `src/config/env.js`
// is permitted to read `import.meta.env` at runtime — everything else imports the
// validated `config` object from there.
interface ImportMetaEnv {
  readonly VITE_DOTNET_URL?: string
  readonly VITE_TELEMETRY_DSN?: string
  readonly VITE_APP_ENV?: 'local' | 'test' | 'staging' | 'production'
  /** Opt-in flag that enables prototype-style demo fixtures (never on in prod). */
  readonly VITE_ENABLE_DEMO_DATA?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
