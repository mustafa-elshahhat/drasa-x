import { useTranslation } from 'react-i18next'
import { DetailList } from '../../../shared/data-display'
import { Card } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { Head, Loading } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { toObject } from '../../../features/student/studentSchemas'
import { settledData } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function ReportsPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSchoolQuery(queryKeys.school.reports(userId), (s) => schoolApi.reports(s))
  const users = query.data ? toObject(settledData(query.data.users)) : null
  const ai = query.data ? toObject(settledData(query.data.ai)) : null
  return (
    <>
      <Head view="reports" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (
        <div className="ui-split ui-split--even">
          <Card title={t('school.reports.users')}>{users ? <DetailList item={users} locale={locale} /> : <EmptyState title={t('school.empty.reports')} />}</Card>
          <Card title={t('school.reports.ai')}>{ai ? <DetailList item={ai} locale={locale} /> : <EmptyState title={t('school.empty.reports')} />}</Card>
        </div>
      )}
    </>
  )
}

export default function SchoolReportsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <ReportsPage userId={userId} locale={locale} {...props} />
}
