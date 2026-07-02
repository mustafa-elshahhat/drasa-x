import { lazy } from 'react'
import type { AppRoute } from './route.types'

const LoginPage = lazy(() => import('../../pages/auth/LoginPage.jsx'))
const ForgotPasswordPage = lazy(() => import('../../pages/auth/ForgotPasswordPage.jsx'))
const ResetPasswordPage = lazy(() => import('../../pages/auth/ResetPasswordPage.jsx'))

export const authRoutes: AppRoute[] = [
  { path: '/login', titleKey: 'auth.title', anonymousOnly: true, Component: LoginPage },
  { path: '/forgot-password', titleKey: 'auth.forgotPasswordTitle', anonymousOnly: true, Component: ForgotPasswordPage },
  { path: '/reset-password', titleKey: 'auth.resetPasswordTitle', anonymousOnly: true, Component: ResetPasswordPage },
]
