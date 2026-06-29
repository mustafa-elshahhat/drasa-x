import { useState } from 'react'
import { BrowserRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../i18n'
import { createQueryClient } from '../lib/query/queryClient'
import { AuthProvider } from '../features/auth/AuthContext'
import { NotificationsProvider } from '../features/notifications/NotificationsProvider'
import { ToastProvider } from '../components/feedback/ToastProvider'
import { ErrorBoundary } from '../components/ErrorBoundary'

// Composition root (Phase 7 §1). Wires every cross-cutting provider in one place
// so the dependency direction is explicit and testable.
//
// Cache isolation (Phase 7 §8): AuthProvider.onSessionEnd clears the entire
// React Query cache on logout/expiry, so no cross-user/cross-tenant data
// survives an account switch.
export function AppProviders({ children }) {
  // One QueryClient for the app lifetime.
  const [queryClient] = useState(() => createQueryClient())

  return (
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <ErrorBoundary name="root">
            <ToastProvider>
              <AuthProvider onSessionEnd={() => queryClient.clear()}>
                <NotificationsProvider>{children}</NotificationsProvider>
              </AuthProvider>
            </ToastProvider>
          </ErrorBoundary>
        </BrowserRouter>
      </QueryClientProvider>
    </I18nextProvider>
  )
}
