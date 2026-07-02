import { useTranslation } from 'react-i18next'
import { Alert } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, List } from '../../../features/system/components'
import { useSystemQuery } from '../../../features/system/helpers'
import { systemApi } from '../../../features/system/systemApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function SecurityPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSystemQuery(queryKeys.system.security(userId), (s) => systemApi.securityEvents(s))
  return (
    <>
      <Head view="security" />
      <Alert title={t('system.notes.securityTitle')}>{t('system.notes.security')}</Alert>
      <List query={query} empty={t('system.empty.security')} locale={locale} />
    </>
  )
}

export default function SystemSecurityPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SecurityPage userId={userId} locale={locale} {...props} />
}
