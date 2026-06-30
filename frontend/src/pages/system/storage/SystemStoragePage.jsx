import { useTranslation } from 'react-i18next'
import { ResourceTable } from '../../../shared/data-display'
import { Alert } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, Loading } from '../../../features/system/components'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function StoragePage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.storage(userId), (s) => systemApi.storage(s))
  const rows = Array.isArray(query.data?.tenants) ? query.data.tenants : []
  return (
    <>
      <Head view="storage" />
      <Alert title={t('system.notes.storageTitle')}>{query.data?.note || t('system.notes.storage')}</Alert>
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && <ResourceTable rows={rows} emptyTitle={t('system.empty.generic')} locale={locale} />}
    </>
  )
}

// ---------------------------------------------------------------------------
// Support inbox (cross-tenant)
// ---------------------------------------------------------------------------

export default function SystemStoragePage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <StoragePage userId={userId} locale={locale} {...props} />
}
