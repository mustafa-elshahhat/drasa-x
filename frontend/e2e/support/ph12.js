// =============================================================================
// Phase 12 System Admin (platform) Portal live-acceptance support. Real browser →
// vite preview → DerasaX-backend → PostgreSQL. No network mocking. Extends the
// Phase 8 helpers (login/nav/guards/reset). The platform admin reads/writes never
// call the AI service directly; the browser never calls AI regardless.
// =============================================================================
export { BACKEND, PASSWORD, RESET_KEY, HAS_CREDS, backendUp, resetE2E, apiLogin, login, nav, setLang, attachGuards, axeSeriousOrCritical } from './ph8.js'

// Deterministic Phase 12 actors (DataSeederService Phase 3 + Phase 12 fixtures).
export const CODES12 = {
  systemAdmin: 'PH12-SYSADMIN', // dedicated platform SystemAdmin (no tenant)
  // wrong-role / regression actors
  student: 'STU-T1',
  teacher: 'TEACH-T1',
  parent: 'PARENT-T1',
  schoolAdmin: 'ADMIN-T1',
}

// Deterministic Phase 12 fixture identifiers.
export const FIX12 = {
  lifecycleTenant: 'PH12-TENANT-SUSPEND', // Active tenant the suspend→reactivate flow drives
  lifecycleTenantName: 'Rosetta Modern School',
  planPro: 'Pro', // seeded SubscriptionPlanDefinition name (PH12-PLAN-PRO)
  supportTicketMessage: "I can't open my child's report card", // seeded Pending ticket in tenant-1
}
