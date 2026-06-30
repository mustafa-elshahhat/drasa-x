import { useTranslation } from 'react-i18next'
import { Alert } from '../../../components/ui/Alert'
import { ErrorState } from '../../../components/ui/states'
import { Head, Loading } from '../../../features/system/components'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function ErrorMonitoringPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.operational(userId), (s) => systemApi.operationalStatus(s))
  return (
    <>
      <Head view="errorMonitoring" />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && (
        <>
          <Alert title={t('system.posture.deferredTitle')}>{t('system.notes.errorMonitoring')}</Alert>
          <PostureCard posture={query.data.errorMonitoring} locale={locale} />
        </>
      )}
    </>
  )
}

export default function SystemErrorMonitoringPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <ErrorMonitoringPage userId={userId} locale={locale} {...props} />
}
