import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SystemSupportPage from './SystemSupportPage'
import { systemApi } from '../../../features/system/systemApi'

// Ported from the retired SystemAdminPortalPage facade ("Phase 12 contracts"): an
// honest empty state renders when there are no support tickets.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'sys-1', fullName: 'Platform Admin' }, role: 'SystemAdmin' }),
}))

vi.mock('../../../features/system/systemApi', () => ({
  systemApi: { supportTickets: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/system/support']}>
          <SystemSupportPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('SystemSupportPage', () => {
  it('shows an empty state when there are no support tickets', async () => {
    systemApi.supportTickets.mockResolvedValue([])
    renderPage()
    expect(await screen.findByText('No support tickets yet.')).toBeInTheDocument()
  })
})
