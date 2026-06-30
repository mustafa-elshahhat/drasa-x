import { useTranslation } from 'react-i18next'
import { DetailList } from '../../../components/data/DetailList'
import { ResourceTable } from '../../../components/data/ResourceTable'
import { Card } from '../../../components/ui/PageHeader'
import { EmptyState, ErrorState } from '../../../components/ui/states'
import { Head, Loading } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { toItems, toObject } from '../../../features/student/studentSchemas'
import { settledData } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function AiUsagePage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSchoolQuery(queryKeys.school.aiUsage(userId), (s) => schoolApi.aiUsage(s))
  const summary = query.data ? toObject(settledData(query.data.summary)) : null
  const list = query.data ? toItems(settledData(query.data.list)) : []
  return (
    <>
      <Head view="aiUsage" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (
        <>
          <Card title={t('school.aiUsage.summary')}>{summary ? <DetailList item={summary} locale={locale} /> : <EmptyState title={t('school.empty.generic')} />}</Card>
          <section className="ui-section">
            <div className="ui-section__head"><h2 className="ui-section__title">{t('school.aiUsage.records')}</h2></div>
            <ResourceTable rows={list} emptyTitle={t('school.empty.generic')} locale={locale} />
          </section>
        </>
      )}
    </>
  )
}

export default function SchoolAiUsagePage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <AiUsagePage userId={userId} locale={locale} {...props} />
}
