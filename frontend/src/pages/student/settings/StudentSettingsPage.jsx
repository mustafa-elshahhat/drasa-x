import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ShieldCheck, Bell } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { Card, PageHeader } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'

function AccountSettingsPage() {
  const { t } = useTranslation()
  return (
    <>
      <PageHeader title={t('student.settings.title')} description={t('student.settings.description')} />
      <div className="ui-split ui-split--even">
        <Card title={t('nav.security')}>
          <Link className="ui-btn ui-btn--primary" to="/app/security"><ShieldCheck size={16} aria-hidden="true" /> {t('student.settings.changePassword')}</Link>
        </Card>
        <Card title={t('student.settings.notifications')}>
          <Link className="ui-btn ui-btn--ghost" to="/app/notifications/preferences"><Bell size={16} aria-hidden="true" /> {t('student.settings.notificationPreferences')}</Link>
        </Card>
      </div>
    </>
  )
}

// =============================================================================
// View dispatcher
// =============================================================================

export default function StudentSettingsPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <AccountSettingsPage userId={userId} locale={locale} {...props} />
}
