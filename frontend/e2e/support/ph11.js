// =============================================================================
// Phase 11 School Admin Portal live-acceptance support. Real browser → vite preview
// → DerasaX-backend → PostgreSQL. No network mocking. Extends the Phase 8/10 helpers
// (login/nav/guards/reset). The School Admin Portal reads/writes never call the AI
// service directly; the browser never calls AI regardless.
// =============================================================================
export { BACKEND, PASSWORD, RESET_KEY, HAS_CREDS, backendUp, resetE2E, apiLogin, login, nav, setLang, attachGuards, axeSeriousOrCritical } from './ph8.js'
export { apiLoginId } from './ph10.js'

// Deterministic Phase 11 actors (DataSeederService Phase 3/8/9/10/11 fixtures).
export const CODES11 = {
  schoolAdmin: 'PH11-SCHOOLADMIN-T1', // tenant-1 SchoolAdmin (primary actor; full data)
  schoolAdminT2: 'PH11-SCHOOLADMIN-T2', // tenant-2 SchoolAdmin (cross-tenant/minimal)
  parentUnlinked: 'PH11-PARENT-T1', // tenant-1 parent with NO links (create flow)
  studentUnlinked: 'PH11-STUDENT-T1', // tenant-1 student with NO links (create flow)
  teacherUnassigned: 'PH11-TEACHER-T1', // tenant-1 teacher with NO class assignment (create flow)
  // wrong-role / regression actors
  student: 'STU-T1',
  teacher: 'TEACH-T1',
  parent: 'PARENT-T1',
  parentLinked: 'PH10-PARENT-T1',
}

// Visible full names of the dedicated create-flow fixtures (for <select> by label).
export const NAMES11 = {
  parentUnlinked: 'Phase 11 Unlinked Parent',
  studentUnlinked: 'Phase 11 Unlinked Student',
  teacherUnassigned: 'Phase 11 Unassigned Teacher',
  classFixture: 'Phase 11 Class',
}
