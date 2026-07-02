import { useState } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { GraduationCap } from 'lucide-react'
import { useAuth } from '../../features/auth/AuthContext'
import { homeRouteForRole } from '../../features/auth/roles'
import { safeReturnPath } from '../../lib/security/returnUrl'
import { useDocumentTitle } from '../../app/useDocumentTitle'
import { PasswordField } from '../../shared/form'
import { Button, Alert } from '../../shared/ui'
import { applyServerErrors } from '../../lib/forms/errors'
import { ApiError } from '../../lib/api/problemDetails'

// Forced password change (first login on a temporary/reset password, or any account still
// flagged mustChangePassword). Reachable only via RequirePasswordChange (guards.jsx) — no
// navigation away from this page is possible except changing the password or logging out.
function buildSchema(t) {
  return z
    .object({
      currentPassword: z.string().min(1),
      newPassword: z.string().min(8, t('security.tooShort', 'New password must be at least 8 characters.')),
      confirmPassword: z.string().min(1),
    })
    .refine((v) => v.newPassword === v.confirmPassword, {
      path: ['confirmPassword'],
      message: t('security.mismatch', 'The new passwords do not match.'),
    })
    .refine((v) => v.newPassword !== v.currentPassword, {
      path: ['newPassword'],
      message: t(
        'forcedChangePassword.samePassword',
        'Your new password must be different from the temporary password.'
      ),
    })
}

export default function ForcedChangePasswordPage() {
  const { changePassword, logout, role } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const { t } = useTranslation()
  const [formError, setFormError] = useState(null)
  useDocumentTitle({ titleKey: 'forcedChangePassword.title' })

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm({ resolver: zodResolver(buildSchema(t)) })

  async function onSubmit(values) {
    setFormError(null)
    try {
      await changePassword({ currentPassword: values.currentPassword, newPassword: values.newPassword })
      // The session was already refreshed (mustChangePassword cleared) inside changePassword().
      const from = safeReturnPath(location.state?.from, homeRouteForRole(role))
      navigate(from, { replace: true })
    } catch (err) {
      const formLevel = applyServerErrors(err instanceof ApiError ? err : new ApiError({ status: 0 }), setError, {
        fieldMap: { CurrentPassword: 'currentPassword', NewPassword: 'newPassword' },
      })
      if (formLevel) setFormError({ message: formLevel, correlationId: err?.correlationId })
    }
  }

  async function onLogout() {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <main className="auth-page" role="main">
      <div className="auth-split">
        <aside className="auth-hero" aria-hidden="true">
          <span className="auth-hero__orb" />
          <div className="auth-hero__brand">
            <GraduationCap size={28} aria-hidden="true" />
            <span>DerasaX</span>
          </div>
          <div className="auth-hero__copy">
            <h2>{t('forcedChangePassword.heroTitle', 'One more step before you continue')}</h2>
            <p>
              {t(
                'forcedChangePassword.heroBody',
                'For your security, temporary and reset passwords must be changed before you can use the app.'
              )}
            </p>
          </div>
          <div className="auth-hero__foot">© 2026 DerasaX</div>
        </aside>

        <div className="auth-panel">
          <div className="auth-card">
            <h1 className="auth-card__title">{t('forcedChangePassword.title', 'Change your password to continue')}</h1>
            <p className="auth-card__sub">
              {t(
                'forcedChangePassword.subtitle',
                'You are signing in with a temporary password. Set a new password before continuing — you will not be able to access the app until this is done.'
              )}
            </p>
            <form onSubmit={handleSubmit(onSubmit)} noValidate>
              {formError && (
                <Alert variant="error" title={formError.message} correlationId={formError.correlationId} />
              )}
              <PasswordField
                label={t('security.currentPassword', 'Current password')}
                autoComplete="current-password"
                required
                error={errors.currentPassword?.message}
                {...register('currentPassword')}
              />
              <PasswordField
                label={t('security.newPassword', 'New password')}
                autoComplete="new-password"
                required
                error={errors.newPassword?.message}
                {...register('newPassword')}
              />
              <PasswordField
                label={t('security.confirmPassword', 'Confirm new password')}
                autoComplete="new-password"
                required
                error={errors.confirmPassword?.message}
                {...register('confirmPassword')}
              />
              <Button type="submit" variant="primary" loading={isSubmitting} className="auth-card__submit">
                {t('forcedChangePassword.submit', 'Change password and continue')}
              </Button>
            </form>
            <p className="auth-card__alt">
              <Button type="button" variant="ghost" onClick={onLogout}>
                {t('actions.signOut', 'Sign out')}
              </Button>
            </p>
          </div>
        </div>
      </div>
    </main>
  )
}
