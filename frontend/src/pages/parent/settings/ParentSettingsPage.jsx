import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ShieldCheck, Bell } from 'lucide-react'
import { Card, PageHeader } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { usePortalContext } from '../../../features/portal/context'

function SettingsPage() {
  const { t } = useTranslation()
  return (
    <>
      <PageHeader title={t('parent.settings.title')} description={t('parent.settings.description')} />
      <div className="ui-split ui-split--even">
        <Card title={t('nav.security')}>
          <Link className="ui-btn ui-btn--primary" to="/app/security"><ShieldCheck size={16} aria-hidden="true" /> {t('parent.settings.changePassword')}</Link>
        </Card>
        <Card title={t('parent.settings.notifications')}>
          <Link className="ui-btn ui-btn--ghost" to="/app/notifications/preferences"><Bell size={16} aria-hidden="true" /> {t('parent.settings.notificationPreferences')}</Link>
        </Card>
      </div>
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
