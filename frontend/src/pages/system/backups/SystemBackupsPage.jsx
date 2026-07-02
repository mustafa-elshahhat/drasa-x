import { useTranslation } from 'react-i18next'
import { Alert } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, Loading, PostureCard } from '../../../features/system/components'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function BackupsPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.operational(userId), (s) => systemApi.operationalStatus(s))
  return (
    <>
      <Head view="backups" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (
        <>
          <Alert title={t('system.posture.deferredTitle')}>{t('system.notes.backups')}</Alert>
          <PostureCard posture={query.data.backups} locale={locale} />
        </>
      )}
    </>
  )
}

// ---------------------------------------------------------------------------

export default function SystemBackupsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <BackupsPage userId={userId} locale={locale} {...props} />
}
