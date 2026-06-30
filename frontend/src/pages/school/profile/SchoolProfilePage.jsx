import { useTranslation } from 'react-i18next'
import { DetailList } from '../../../shared/data-display'
import { Card } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, Loading } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function ProfilePage({ userId, locale }) {
  const query = useSchoolQuery(queryKeys.school.profile(userId), (s) => schoolApi.profile(s))
  const { t } = useTranslation()
  return (
    <>
      <Head view="profile" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && <Card title={query.data.name || t('school.pages.profile.title')}><DetailList item={query.data} locale={locale} /></Card>}
    </>
  )
}

export default function SchoolProfilePage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <ProfilePage userId={userId} locale={locale} {...props} />
}
