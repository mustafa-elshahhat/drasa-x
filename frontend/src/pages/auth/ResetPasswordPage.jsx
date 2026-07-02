import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { GraduationCap } from 'lucide-react'
import { resetPassword } from '../../features/auth/authApi'
import { useDocumentTitle } from '../../app/useDocumentTitle'
import { TextField, PasswordField } from '../../shared/form'
import { Button, Alert } from '../../shared/ui'
import { ErrorState } from '../../shared/feedback'
import { LanguageSwitcher } from '../../layouts/LanguageSwitcher'

// Reset-password screen wired to the real DerasaX-backend account contract
// (POST /api/v1/account/reset-password). Fails with a 400 "Invalid or expired
// reset token." (surfaced via ErrorState) when the token is wrong/expired/reused.
function buildSchema(t) {
  return z
    .object({
      loginCode: z.string().min(1),
      token: z.string().min(1),
      newPassword: z.string().min(8, t('auth.passwordTooShort')),
      confirmPassword: z.string().min(1),
    })
    .refine((v) => v.newPassword === v.confirmPassword, {
      path: ['confirmPassword'],
      message: t('auth.passwordMismatch'),
    })
}

export default function ResetPasswordPage() {
  const { t } = useTranslation()
  const [searchParams] = useSearchParams()
  const [success, setSuccess] = useState(false)
  const [error, setError] = useState(null)
  useDocumentTitle({ titleKey: 'auth.resetPasswordTitle' })

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm({
    resolver: zodResolver(buildSchema(t)),
    defaultValues: {
      // Convenience pre-fill from the dev-only forgot-password link; harmless no-op
      // in production where no devToken is ever issued.
      loginCode: searchParams.get('loginCode') || '',
      token: searchParams.get('token') || '',
      newPassword: '',
      confirmPassword: '',
    },
  })

  async function onSubmit(values) {
    setError(null)
    const outcome = await resetPassword({
      loginCode: values.loginCode.trim(),
      token: values.token.trim(),
      newPassword: values.newPassword,
    })
    if (!outcome.ok) {
      setError(outcome.error)
      return
    }
    setSuccess(true)
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
            <h2>{t('auth.welcomeTitle', 'Welcome back to DerasaX')}</h2>
            <p>{t('auth.welcomeBody', 'Sign in to continue your AI-powered learning journey.')}</p>
          </div>
          <div className="auth-hero__foot">© 2026 DerasaX</div>
        </aside>

        <div className="auth-panel">
          <div className="auth-card">
            <div className="auth-card__top">
              <LanguageSwitcher />
            </div>
            <h1 className="auth-card__title">{t('auth.resetPasswordTitle')}</h1>
            <p className="auth-card__sub">{t('auth.resetPasswordSubtitle')}</p>

            {!success && (
              <form onSubmit={handleSubmit(onSubmit)} noValidate>
                {error && <ErrorState error={error} onRetry={() => setError(null)} />}
                <TextField
                  label={t('auth.loginCode', 'Login code')}
                  autoComplete="username"
                  required
                  error={errors.loginCode && t('auth.missingFields')}
                  {...register('loginCode')}
                />
                <TextField
                  label={t('auth.resetToken')}
                  required
                  error={errors.token && t('auth.missingFields')}
                  {...register('token')}
                />
                <PasswordField
                  label={t('auth.newPassword')}
                  autoComplete="new-password"
                  required
                  error={errors.newPassword?.message}
                  {...register('newPassword')}
                />
                <PasswordField
                  label={t('auth.confirmNewPassword')}
                  autoComplete="new-password"
                  required
                  error={errors.confirmPassword?.message}
                  {...register('confirmPassword')}
                />
                <Button type="submit" variant="primary" loading={isSubmitting} className="auth-card__submit">
                  {t('auth.resetPasswordSubmit')}
                </Button>
              </form>
            )}

            {success && (
              <Alert variant="success" title={t('auth.resetPasswordSuccess')} />
            )}

            <p className="auth-card__alt">
              <Link to="/login">{t('auth.backToLogin')}</Link>
            </p>
          </div>
        </div>
      </div>
    </main>
  )
}
