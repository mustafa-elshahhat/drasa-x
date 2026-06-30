import { useTranslation } from 'react-i18next'
import { Bot, GraduationCap, Users } from 'lucide-react'
import { ResourceTable } from '../../../components/data/ResourceTable'
import { Metric } from '../../../components/ui/Metric'
import { ErrorState } from '../../../components/ui/states'
import { Head, Loading } from '../../../features/system/components'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function UsagePage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.usage(userId), (s) => systemApi.usage(s))
  const rows = Array.isArray(query.data?.tenants) ? query.data.tenants : []
  return (
    <>
      <Head view="usage" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (
        <>
          <div className="student-dashboard">
            <Metric icon={GraduationCap} accent="var(--brand)" value={query.data.totalStudents ?? 0} label={t('roles.Student')} />
            <Metric icon={Users} accent="var(--purple)" value={query.data.totalTeachers ?? 0} label={t('roles.Teacher')} />
            <Metric icon={Bot} accent="var(--purple)" value={query.data.totalAiGenerations ?? 0} label={t('system.dashboard.aiRecords')} />
          </div>
          <section className="ui-section">
            <div className="ui-section__head"><h2 className="ui-section__title">{t('system.pages.usage.title')}</h2></div>
            <ResourceTable rows={rows} emptyTitle={t('system.empty.usage')} locale={locale} />
          </section>
        </>
      )}
    </>
  )
}

export default function SystemUsagePage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <UsagePage userId={userId} locale={locale} {...props} />
}
