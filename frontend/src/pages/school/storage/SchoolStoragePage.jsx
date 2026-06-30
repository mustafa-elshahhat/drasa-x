import { useTranslation } from 'react-i18next'
import { DetailList } from '../../../shared/data-display'
import { Alert, Card } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, Loading } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function StoragePage({ userId, locale }) {
  const usage = useSchoolQuery(queryKeys.school.usage(userId), (s) => schoolApi.usage(s))
  const { t } = useTranslation()
  return (
    <>
      <Head view="storage" />
      <Alert title={t('school.notes.storageTitle')}>{t('school.notes.storage')}</Alert>
      {usage.isLoading && <Loading />}
      {usage.isError && <ErrorState error={usage.error} onRetry={usage.refetch} />}
      {usage.data && <Card title={t('school.pages.storage.title')}><DetailList item={usage.data} locale={locale} /></Card>}
    </>
  )
}

// ---------------------------------------------------------------------------
// Academic structure (inline create + typed list)
// ---------------------------------------------------------------------------

export default function SchoolStoragePage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <StoragePage userId={userId} locale={locale} {...props} />
}
