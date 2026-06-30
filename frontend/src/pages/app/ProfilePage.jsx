import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { ShieldCheck } from 'lucide-react'
import { useAuth } from '../../features/auth/AuthContext'
import { useDocumentTitle } from '../../app/useDocumentTitle'
import { PageHeader, Card } from '../../components/ui/PageHeader'
import { Avatar } from '../../components/ui/Avatar'
import { Chip } from '../../components/ui/Chip'

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
      <div className="ui-split">
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
        <Card>
          <div className="profile-identity">
            <Avatar name={user?.fullName} size={72} />
            <h2 className="profile-identity__name">{user?.fullName || '—'}</h2>
            {role && <Chip tone="accent">{t(`roles.${role}`, role)}</Chip>}
            <Link to="/app/security" className="ui-btn ui-btn--secondary ui-btn--block">
              <ShieldCheck size={16} aria-hidden="true" /> {t('nav.security')}
            </Link>
          </div>
        </Card>
      </div>
    </>
  )
}
