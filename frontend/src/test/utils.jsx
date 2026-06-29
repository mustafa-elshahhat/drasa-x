import { render } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../i18n'
import { createQueryClient } from '../lib/query/queryClient'
import { ToastProvider } from '../components/feedback/ToastProvider'

// Test render helper that wraps a component in the app's real providers
// (i18n + React Query + Router + Toasts) so component/integration tests run
// against the actual cross-cutting infrastructure (Phase 7 §22).
export function renderWithProviders(ui, { route = '/', queryClient } = {}) {
  const client = queryClient || createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <ToastProvider>
          <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
        </ToastProvider>
      </QueryClientProvider>
    </I18nextProvider>
  )
}

export { i18n }
