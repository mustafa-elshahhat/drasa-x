import { useTranslation } from 'react-i18next'
import { DetailList } from '../../../shared/data-display'
import { Card } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, Loading } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function SubscriptionPage({ userId, locale }) {
  const sub = useSchoolQuery(queryKeys.school.subscription(userId), (s) => schoolApi.subscription(s))
  const { t } = useTranslation()
  return (
    <>
      <Head view="subscription" />
      {sub.isLoading && <Loading />}
      {sub.isError && <ErrorState error={sub.error} onRetry={sub.refetch} />}
      {sub.data && <Card title={t('school.pages.subscription.title')}><DetailList item={sub.data} locale={locale} /></Card>}
    </>
  )
}

export default function SchoolSubscriptionPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SubscriptionPage userId={userId} locale={locale} {...props} />
}
