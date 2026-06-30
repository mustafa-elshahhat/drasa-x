import { useTranslation } from 'react-i18next'
import { DetailList } from '../../../components/data/DetailList'
import { Alert } from '../../../components/ui/Alert'
import { Card } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
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

// ---------------------------------------------------------------------------
// Operational posture: Health / Error monitoring / Backups
// ---------------------------------------------------------------------------
function PostureCard({ posture, locale }) {
  const { t } = useTranslation()
  if (!posture) return null
  return (
    <Card title={posture.configured ? t('system.posture.configured') : t('system.posture.notConfigured')}>
      <DetailList item={posture} locale={locale} />
    </Card>
  )
}

export default function SystemSecurityPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SecurityPage userId={userId} locale={locale} {...props} />
}
