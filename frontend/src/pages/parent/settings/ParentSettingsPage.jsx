import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ShieldCheck } from 'lucide-react'
import { Card, PageHeader } from '../../../components/ui/PageHeader'
import { ErrorState } from '../../../components/ui/states'
import { usePortalContext } from '../../../features/portal/context'

function SettingsPage() {
  const { t } = useTranslation()
  return (
    <>
      <PageHeader title={t('parent.settings.title')} description={t('parent.settings.description')} />
      <Card title={t('nav.security')}>
        <Link className="ui-btn ui-btn--primary" to="/app/security"><ShieldCheck size={16} aria-hidden="true" /> {t('parent.settings.changePassword')}</Link>
      </Card>
    </>
  )
}

export default function ParentSettingsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SettingsPage userId={userId} locale={locale} {...props} />
}
