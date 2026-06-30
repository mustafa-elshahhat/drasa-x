import SystemDashboardPage from './dashboard/SystemDashboardPage'
import SystemTenantsPage from './tenants/SystemTenantsPage'
import SystemTenantDetailsPage from './tenants/SystemTenantDetailsPage'
import SystemOnboardingPage from './onboarding/SystemOnboardingPage'
import SystemPlansPage from './plans/SystemPlansPage'
import SystemSubscriptionsPage from './subscriptions/SystemSubscriptionsPage'
import SystemUsagePage from './usage/SystemUsagePage'
import SystemAiUsagePage from './ai-usage/SystemAiUsagePage'
import SystemStoragePage from './storage/SystemStoragePage'
import SystemSupportPage from './support/SystemSupportPage'
import SystemFeatureFlagsPage from './feature-flags/SystemFeatureFlagsPage'
import SystemAnnouncementsPage from './announcements/SystemAnnouncementsPage'
import SystemSettingsPage from './settings/SystemSettingsPage'
import SystemAuditPage from './audit/SystemAuditPage'
import SystemSecurityPage from './security/SystemSecurityPage'
import SystemHealthPage from './health/SystemHealthPage'
import SystemErrorMonitoringPage from './errors/SystemErrorMonitoringPage'
import SystemBackupsPage from './backups/SystemBackupsPage'

// TEMPORARY compatibility facade (Phase 7 split). The system-admin dispatcher was
// split into the real, lazily-loaded page modules above; routes.system.tsx loads
// them directly. Kept only for the legacy view-based test harness.
const VIEWS = {
  tenants: SystemTenantsPage,
  'tenant-details': SystemTenantDetailsPage,
  onboarding: SystemOnboardingPage,
  plans: SystemPlansPage,
  subscriptions: SystemSubscriptionsPage,
  usage: SystemUsagePage,
  'ai-usage': SystemAiUsagePage,
  storage: SystemStoragePage,
  support: SystemSupportPage,
  'feature-flags': SystemFeatureFlagsPage,
  announcements: SystemAnnouncementsPage,
  health: SystemHealthPage,
  errors: SystemErrorMonitoringPage,
  audit: SystemAuditPage,
  security: SystemSecurityPage,
  backups: SystemBackupsPage,
  settings: SystemSettingsPage,
}

export default function SystemAdminPortalPage({ view }) {
  const Page = VIEWS[view] || SystemDashboardPage
  return <Page />
}
