import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SystemErrorMonitoringPage from './SystemErrorMonitoringPage'
import { systemApi } from '../../../features/system/systemApi'

// Regression test for F-02 / P0-2: `PostureCard` was referenced but never
// imported, so this page threw `ReferenceError: PostureCard is not defined`
// as soon as operational-status data loaded.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'sys-1', fullName: 'Platform Admin' }, role: 'SystemAdmin' }),
}))

vi.mock('../../../features/system/systemApi', () => ({
  systemApi: { operationalStatus: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/system/errors']}>
          <SystemErrorMonitoringPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('SystemErrorMonitoringPage', () => {
  it('renders the posture card without crashing once data loads', async () => {
    systemApi.operationalStatus.mockResolvedValue({ errorMonitoring: { configured: false, note: 'Deferred' } })
    renderPage()
    expect(await screen.findByRole('heading', { name: 'Not configured' })).toBeInTheDocument()
  })
})
