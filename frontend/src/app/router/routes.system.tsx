import { lazy } from 'react'
import type { AppRoute, RouteComponent } from './route.types'
import { ROLES } from '../../features/auth/roles'

// System Admin (platform) portal (Phase 12 surface) — real lazy-loaded page
// modules (Phase 7 split). Platform-admin surfaces; never reachable by a tenant role.
const SystemDashboardPage = lazy(() => import('../../pages/system/dashboard/SystemDashboardPage'))
const SystemTenantsPage = lazy(() => import('../../pages/system/tenants/SystemTenantsPage'))
const SystemTenantDetailsPage = lazy(() => import('../../pages/system/tenants/SystemTenantDetailsPage'))
const SystemOnboardingPage = lazy(() => import('../../pages/system/onboarding/SystemOnboardingPage'))
const SystemPlansPage = lazy(() => import('../../pages/system/plans/SystemPlansPage'))
const SystemSubscriptionsPage = lazy(() => import('../../pages/system/subscriptions/SystemSubscriptionsPage'))
const SystemUsagePage = lazy(() => import('../../pages/system/usage/SystemUsagePage'))
const SystemAiUsagePage = lazy(() => import('../../pages/system/ai-usage/SystemAiUsagePage'))
const SystemStoragePage = lazy(() => import('../../pages/system/storage/SystemStoragePage'))
const SystemSupportPage = lazy(() => import('../../pages/system/support/SystemSupportPage'))
const SystemFeatureFlagsPage = lazy(() => import('../../pages/system/feature-flags/SystemFeatureFlagsPage'))
const SystemAnnouncementsPage = lazy(() => import('../../pages/system/announcements/SystemAnnouncementsPage'))
const SystemSettingsPage = lazy(() => import('../../pages/system/settings/SystemSettingsPage'))
const SystemAuditPage = lazy(() => import('../../pages/system/audit/SystemAuditPage'))
const SystemSecurityPage = lazy(() => import('../../pages/system/security/SystemSecurityPage'))
const SystemHealthPage = lazy(() => import('../../pages/system/health/SystemHealthPage'))
const SystemErrorMonitoringPage = lazy(() => import('../../pages/system/errors/SystemErrorMonitoringPage'))
const SystemBackupsPage = lazy(() => import('../../pages/system/backups/SystemBackupsPage'))

const r = (path: string, titleKey: string, Component: RouteComponent): AppRoute => ({
  path,
  titleKey,
  requiresAuth: true,
  roles: [ROLES.SYSTEM_ADMIN],
  Component,
})

export const systemRoutes: AppRoute[] = [
  r('/app/system', 'system.pages.dashboard.title', SystemDashboardPage),
  r('/app/system/tenants', 'system.pages.tenants.title', SystemTenantsPage),
  r('/app/system/tenants/:tenantId', 'system.pages.tenantDetails.title', SystemTenantDetailsPage),
  r('/app/system/onboarding', 'system.pages.onboarding.title', SystemOnboardingPage),
  r('/app/system/plans', 'system.pages.plans.title', SystemPlansPage),
  r('/app/system/subscriptions', 'system.pages.subscriptions.title', SystemSubscriptionsPage),
  r('/app/system/usage', 'system.pages.usage.title', SystemUsagePage),
  r('/app/system/ai-usage', 'system.pages.aiUsage.title', SystemAiUsagePage),
  r('/app/system/storage', 'system.pages.storage.title', SystemStoragePage),
  r('/app/system/support', 'system.pages.support.title', SystemSupportPage),
  r('/app/system/feature-flags', 'system.pages.featureFlags.title', SystemFeatureFlagsPage),
  r('/app/system/announcements', 'system.pages.announcements.title', SystemAnnouncementsPage),
  r('/app/system/health', 'system.pages.health.title', SystemHealthPage),
  r('/app/system/errors', 'system.pages.errorMonitoring.title', SystemErrorMonitoringPage),
  r('/app/system/audit', 'system.pages.audit.title', SystemAuditPage),
  r('/app/system/security', 'system.pages.security.title', SystemSecurityPage),
  r('/app/system/backups', 'system.pages.backups.title', SystemBackupsPage),
  r('/app/system/settings', 'system.pages.settings.title', SystemSettingsPage),
]
