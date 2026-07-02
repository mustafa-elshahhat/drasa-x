import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { GraduationCap } from 'lucide-react'
import { forgotPassword } from '../../features/auth/authApi'
import { useDocumentTitle } from '../../app/useDocumentTitle'
import { TextField } from '../../shared/form'
import { Button, Alert } from '../../shared/ui'
import { ErrorState } from '../../shared/feedback'
import { LanguageSwitcher } from '../../layouts/LanguageSwitcher'

// Forgot-password screen wired to the real DerasaX-backend account contract
// (POST /api/v1/account/forgot-password). The backend ALWAYS returns 200 with the
// same generic response shape regardless of whether the login code belongs to a
// real account — this page must never branch its message on account existence.
const schema = z.object({
  loginCode: z.string().min(1),
})

export default function ForgotPasswordPage() {
  const { t } = useTranslation()
  const [result, setResult] = useState(null) // { loginCode, devToken } once submitted successfully
  const [error, setError] = useState(null)
  useDocumentTitle({ titleKey: 'auth.forgotPasswordTitle' })

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm({ resolver: zodResolver(schema), defaultValues: { loginCode: '' } })

  async function onSubmit(values) {
    setError(null)
    const loginCode = values.loginCode.trim()
    const outcome = await forgotPassword(loginCode)
    if (!outcome.ok) {
      setError(outcome.error)
      return
    }
    // Always the same generic outcome — the backend never discloses account existence.
    setResult({ loginCode, devToken: outcome.devToken || null })
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
            <h1 className="auth-card__title">{t('auth.forgotPasswordTitle')}</h1>
            <p className="auth-card__sub">{t('auth.forgotPasswordSubtitle')}</p>

            {!result && (
              <form onSubmit={handleSubmit(onSubmit)} noValidate>
                {error && <ErrorState error={error} onRetry={() => setError(null)} />}
                <TextField
                  label={t('auth.loginCode', 'Login code')}
                  autoComplete="username"
                  required
                  error={errors.loginCode && t('auth.missingFields')}
                  {...register('loginCode')}
                />
                <Button type="submit" variant="primary" loading={isSubmitting} className="auth-card__submit">
                  {t('auth.forgotPasswordSubmit')}
                </Button>
              </form>
            )}

            {result && (
              <>
                <Alert variant="success" title={t('auth.forgotPasswordSuccess')} />
                {result.devToken && (
                  <Alert variant="info" title={t('auth.devTokenTitle')}>
                    <p>{t('auth.devTokenBody')}</p>
                    <p><code>{result.devToken}</code></p>
                    <Link
                      to={`/reset-password?loginCode=${encodeURIComponent(result.loginCode)}&token=${encodeURIComponent(result.devToken)}`}
                    >
                      {t('auth.devTokenLink')}
                    </Link>
                  </Alert>
                )}
              </>
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
