import { useTranslation } from 'react-i18next'
import { useAuth } from '../../features/auth/AuthContext'
import { useDocumentTitle } from '../../app/useDocumentTitle'
import { PageHeader, Card } from '../../components/ui/PageHeader'

// Profile page (Phase 7). Renders ONLY real identity derived from the signed-in
// session — no hardcoded user, tenant, or fabricated stats.
export default function ProfilePage() {
  const { user, role } = useAuth()
  const { t } = useTranslation()
  useDocumentTitle({ titleKey: 'profile.title' })

  const rows = [
    { label: t('profile.name', 'Name'), value: user?.fullName },
    { label: t('profile.loginName', 'Login name'), value: user?.userName },
    { label: t('profile.role', 'Role'), value: role ? t(`roles.${role}`, role) : '—' },
    { label: t('profile.userId', 'User ID'), value: user?.id },
  ]

  return (
    <>
      <PageHeader title={t('profile.title', 'Your profile')} description={t('profile.description')} />
      <Card>
        <dl className="profile-list">
          {rows.map((r) => (
            <div key={r.label} className="profile-list__row">
              <dt>{r.label}</dt>
              <dd>{r.value || '—'}</dd>
            </div>
          ))}
        </dl>
      </Card>
    </>
  )
}
