import { lazy } from 'react'
import type { AppRoute } from './route.types'

const LoginPage = lazy(() => import('../../pages/auth/LoginPage.jsx'))

export const authRoutes: AppRoute[] = [
  { path: '/login', titleKey: 'auth.title', anonymousOnly: true, Component: LoginPage },
]
