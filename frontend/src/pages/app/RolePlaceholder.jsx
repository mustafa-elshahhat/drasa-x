import { useTranslation } from 'react-i18next'
import { Construction } from 'lucide-react'
import { useAuth } from '../../features/auth/AuthContext'
import { useDocumentTitle } from '../../app/useDocumentTitle'
import { PageHeader, Card } from '../../components/ui/PageHeader'

// Intentional, clearly-marked route placeholder for a role area whose full
// portal lands in a later phase (Phase 7 §7). It proves role-aware routing and
// renders the real signed-in context, but DELIBERATELY shows NO fabricated
// statistics, records, or completion states.
const AREA_TITLES = {
  student: 'nav.myLearning',
  teacher: 'nav.classAnalytics',
  assignments: 'nav.assignments',
  parent: 'nav.childProgress',
  school: 'nav.schoolAdmin',
  system: 'nav.systemAdmin',
}

export default function RolePlaceholder({ area, phase }) {
  const { user, role } = useAuth()
  const { t } = useTranslation()
  const titleKey = AREA_TITLES[area] || 'nav.dashboard'
  useDocumentTitle({ titleKey })

  return (
    <>
      <PageHeader title={t(titleKey)} description={t('app.tagline')} />
      <Card>
        <div className="placeholder" role="status">
          <Construction size={36} aria-hidden="true" className="placeholder__icon" />
          <p className="placeholder__note">{t('pages.placeholderNote')}</p>
          <p className="placeholder__meta">
            {t('profile.role', 'Role')}: <strong>{role ? t(`roles.${role}`, role) : '—'}</strong>
            {' · '}
            {t('profile.name', 'Name')}: <strong>{user?.fullName || user?.userName || '—'}</strong>
          </p>
          {phase && <p className="placeholder__phase">{phase}</p>}
        </div>
      </Card>
    </>
  )
}
