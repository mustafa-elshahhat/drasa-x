import { useState } from 'react'
import { Link, useNavigate, useLocation } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { GraduationCap } from 'lucide-react'
import { useAuth } from '../../features/auth/AuthContext'
import { homeRouteForRole } from '../../features/auth/roles'
import { safeReturnPath } from '../../lib/security/returnUrl'
import { useDocumentTitle } from '../../app/useDocumentTitle'
import { TextField, PasswordField } from '../../components/form/fields'
import { Button } from '../../components/ui/Button'
import { Alert } from '../../components/ui/Alert'
import { LanguageSwitcher } from '../../layouts/LanguageSwitcher'

// Login screen wired to the real DerasaX-backend account contract (Phase 7
// §4/§9). Accessible labels, validation, loading + generic error states. No
// account-enumeration messages, no mock success.
const schema = z.object({
  loginCode: z.string().min(1),
  password: z.string().min(1),
})

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const { t } = useTranslation()
  const [formError, setFormError] = useState('')
  useDocumentTitle({ titleKey: 'auth.title' })

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm({ resolver: zodResolver(schema), defaultValues: { loginCode: '', password: '' } })

  async function onSubmit(values) {
    setFormError('')
    const result = await login(values.loginCode.trim(), values.password)
    if (result.suspended) {
      navigate('/suspended', { replace: true })
      return
    }
    if (!result.ok) {
      // All failures surface the same generic message (no enumeration).
      setFormError(t('auth.invalidCredentials', 'Invalid login code or password.'))
      return
    }
    const from = safeReturnPath(location.state?.from, homeRouteForRole(result.session?.role))
    navigate(from, { replace: true })
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
            <h1 className="auth-card__title">{t('auth.title', 'Sign in to DerasaX')}</h1>
            <p className="auth-card__sub">{t('auth.subtitle', 'Enter your credentials to access your portal.')}</p>
            <form onSubmit={handleSubmit(onSubmit)} noValidate>
              {formError && <Alert variant="error" title={formError} />}
              <TextField
                label={t('auth.loginCode', 'Login code')}
                autoComplete="username"
                required
                error={errors.loginCode && t('auth.missingFields')}
                {...register('loginCode')}
              />
              <PasswordField
                label={t('auth.password', 'Password')}
                autoComplete="current-password"
                required
                error={errors.password && t('auth.missingFields')}
                {...register('password')}
              />
              <Button type="submit" variant="primary" loading={isSubmitting} className="auth-card__submit">
                {isSubmitting ? t('auth.signingIn', 'Signing in…') : t('actions.signIn', 'Sign in')}
              </Button>
            </form>
            <p className="auth-card__alt">
              {t('auth.noAccount', 'No account?')}{' '}
              <Link to="/request-demo">{t('public.requestDemo.title', 'Request a demo')}</Link>
            </p>
          </div>
        </div>
      </div>
    </main>
  )
}
