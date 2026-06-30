import { useTranslation } from 'react-i18next'
import { Bot, Building2, CreditCard, GraduationCap, Grid3x3, MessageSquare, ScrollText, UserCog, Users } from 'lucide-react'
import { ResourceTable, Metric } from '../../../shared/data-display'
import { Alert } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, Loading } from '../../../features/system/components'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function DashboardPage({ userId, locale }) {
  const { t } = useTranslation()
  const dashboard = useSystemQuery(queryKeys.system.dashboard(userId), (s) => systemApi.dashboard(s))
  const d = dashboard.data || {}
  const recent = Array.isArray(d.recentActivity) ? d.recentActivity : []
  return (
    <>
      <Head view="dashboard" />
      {dashboard.isLoading && <Loading />}
      {dashboard.isError && <ErrorState error={dashboard.error} onRetry={dashboard.refetch} />}
      {dashboard.data && (
        <>
          <Alert variant="info" title={t('system.dashboard.tenants')}>
            {t('system.dashboard.active')}: <strong>{d.tenantsActive ?? 0}</strong> ·{' '}
            {t('system.dashboard.suspended')}: <strong>{d.tenantsSuspended ?? 0}</strong> ·{' '}
            {t('system.dashboard.archived')}: <strong>{d.tenantsArchived ?? 0}</strong>
          </Alert>
          <div className="student-dashboard">
            <Metric to="/app/system/tenants" icon={Building2} accent="var(--success)" value={d.tenantsTotal ?? 0} label={t('system.pages.tenants.title')} />
            <Metric to="/app/system/usage" icon={GraduationCap} accent="var(--brand)" value={d.students ?? 0} label={t('roles.Student')} />
            <Metric to="/app/system/usage" icon={Users} accent="var(--purple)" value={d.teachers ?? 0} label={t('roles.Teacher')} />
            <Metric to="/app/system/usage" icon={UserCog} accent="var(--info)" value={d.schoolAdmins ?? 0} label={t('roles.SchoolAdmin')} />
            <Metric to="/app/system/plans" icon={Grid3x3} accent="var(--orange)" value={d.plansTotal ?? 0} label={t('system.pages.plans.title')} />
            <Metric to="/app/system/subscriptions" icon={CreditCard} accent="var(--success)" value={d.subscriptionsTotal ?? 0} label={t('system.pages.subscriptions.title')} />
            <Metric to="/app/system/ai-usage" icon={Bot} accent="var(--purple)" value={d.aiUsageRecords ?? 0} label={t('system.dashboard.aiRecords')} sub={t('system.dashboard.aiTokens', { tokens: d.aiTotalTokens ?? 0 })} />
            <Metric to="/app/system/support" icon={MessageSquare} accent="var(--warning)" value={d.supportOpen ?? 0} label={t('system.pages.support.title')} />
            <Metric to="/app/system/audit" icon={ScrollText} accent="var(--info)" value={d.recentAuditEvents ?? 0} label={t('system.pages.audit.title')} />
          </div>
          <section className="ui-section">
            <div className="ui-section__head"><h2 className="ui-section__title">{t('system.dashboard.recentActivity')}</h2></div>
            <ResourceTable rows={recent} emptyTitle={t('system.empty.audit')} locale={locale} />
          </section>
        </>
      )}
    </>
  )
}

// ---------------------------------------------------------------------------
// Tenants list (+ status filter)
// ---------------------------------------------------------------------------

export default function SystemDashboardPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <DashboardPage userId={userId} locale={locale} {...props} />
}
