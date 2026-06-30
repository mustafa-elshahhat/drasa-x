import { config } from '../config/env'

// =============================================================================
// Demo-data gate. The ONLY switch that turns on prototype-style sample data.
//
// Driven by VITE_ENABLE_DEMO_DATA (validated in src/config/env.js) and FORCED
// OFF in production. Production UI therefore never presents sample/fixture data
// as real backend data — every call site that previously used a mock fallback
// now renders an honest loading/empty/error state unless demo mode is on.
// =============================================================================
export function isDemoEnabled(): boolean {
  return config.enableDemoData === true
}
