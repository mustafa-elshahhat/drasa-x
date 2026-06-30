import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ShieldCheck } from 'lucide-react'
import { Card, PageHeader } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { usePortalContext } from '../../../features/portal/context'

function SettingsPage() {
  const { t } = useTranslation()
  return (
    <>
      <PageHeader title={t('teacher.settings.title')} description={t('teacher.settings.description')} />
      <Card title={t('nav.security')}>
        <Link className="ui-btn ui-btn--primary" to="/app/security"><ShieldCheck size={16} aria-hidden="true" /> {t('teacher.settings.changePassword')}</Link>
      </Card>
    </>
  )
}

export default function TeacherSettingsPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <SettingsPage userId={userId} locale={locale} {...props} />
}
