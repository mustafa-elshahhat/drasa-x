import { useTranslation } from 'react-i18next'
import { DetailList } from '../../../shared/data-display'
import { Alert, Card } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, Loading } from '../../../features/system/components'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function HealthPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.operational(userId), (s) => systemApi.operationalStatus(s))
  const h = query.data?.health
  return (
    <>
      <Head view="health" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (
        <>
          <Alert variant={h?.databaseReachable ? 'success' : 'error'} title={t('system.health.title')}>
            {t('system.health.api')}: <strong>{h?.api}</strong> · {t('system.health.database')}: <strong>{h?.databaseReachable ? t('system.health.reachable') : t('system.health.unreachable')}</strong>
          </Alert>
          <Card title={t('system.pages.health.title')}><DetailList item={h} locale={locale} /></Card>
          {/* Phase 19 — dependency health (storage / AI / background jobs), request metrics, deployment. */}
          {query.data.storage && <Card title={t('system.health.storage')}><DetailList item={query.data.storage} locale={locale} /></Card>}
          {query.data.aiService && <Card title={t('system.health.aiService')}><DetailList item={query.data.aiService} locale={locale} /></Card>}
          {query.data.backgroundJobs && <Card title={t('system.health.backgroundJobs')}><DetailList item={query.data.backgroundJobs} locale={locale} /></Card>}
          {query.data.metrics && <Card title={t('system.health.metrics')}><DetailList item={query.data.metrics} locale={locale} /></Card>}
          {(query.data.version || query.data.environment) && (
            <Card title={t('system.health.deployment')}>
              <DetailList item={{ version: query.data.version, environment: query.data.environment, uptimeSeconds: query.data.uptimeSeconds }} locale={locale} />
            </Card>
          )}
        </>
      )}
    </>
  )
}

export default function SystemHealthPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <HealthPage userId={userId} locale={locale} {...props} />
}
