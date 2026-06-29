// =============================================================================
// Service-worker registration (Phase 7 §17). Uses a "prompt" update strategy so
// a new version never silently swaps under the user. Registration is a no-op in
// dev (the SW is disabled in vite.config) and only runs in the browser.
//
// Safety: the SW precaches only static assets and uses NetworkOnly for /api/**
// (see vite.config), so no authenticated/token-bearing response is ever cached.
// =============================================================================
export async function registerPwa() {
  if (typeof window === 'undefined') return
  try {
    const { registerSW } = await import('virtual:pwa-register')
    registerSW({
      immediate: true,
      onNeedRefresh() {
        // A new version is available. In a later phase this can surface a toast
        // with an "Update" action; for now we let the next navigation pick it up.
        if (import.meta.env.DEV) console.info('[pwa] update available')
      },
      onOfflineReady() {
        if (import.meta.env.DEV) console.info('[pwa] offline-ready (static shell)')
      },
    })
  } catch {
    // virtual module unavailable (e.g. SW disabled) — nothing to register.
  }
}
