import { useTranslation } from 'react-i18next'
import { Bot } from 'lucide-react'
import { ResourceTable, Metric } from '../../../shared/data-display'
import { ErrorState } from '../../../shared/feedback'
import { Head, Loading } from '../../../features/system/components'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function AiUsagePage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.aiUsage(userId), (s) => systemApi.aiUsage(s))
  const rows = Array.isArray(query.data?.tenants) ? query.data.tenants : []
  return (
    <>
      <Head view="aiUsage" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (
        <>
          <div className="student-dashboard">
            <Metric icon={Bot} accent="var(--purple)" value={query.data.records ?? 0} label={t('system.aiUsage.records')} />
            <Metric icon={Bot} accent="var(--brand)" value={query.data.totalTokens ?? 0} label={t('system.aiUsage.tokens')} />
          </div>
          <section className="ui-section">
            <div className="ui-section__head"><h2 className="ui-section__title">{t('system.aiUsage.byTenant')}</h2></div>
            <ResourceTable rows={rows} emptyTitle={t('system.empty.generic')} locale={locale} />
          </section>
        </>
      )}
    </>
  )
}

export default function SystemAiUsagePage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <AiUsagePage userId={userId} locale={locale} {...props} />
}
