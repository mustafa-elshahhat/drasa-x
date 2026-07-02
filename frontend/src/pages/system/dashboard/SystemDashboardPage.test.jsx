import { describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SystemDashboardPage from './SystemDashboardPage'
import { systemApi } from '../../../features/system/systemApi'
import { ApiError } from '../../../lib/api/problemDetails'

// Ported from the retired SystemAdminPortalPage facade ("Phase 12 contracts"): the
// dashboard renders the real platform-wide aggregate, and surfaces an honest
// error/retry state when the read is denied.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'sys-1', fullName: 'Platform Admin' }, role: 'SystemAdmin' }),
}))

vi.mock('../../../features/system/systemApi', () => ({
  systemApi: { dashboard: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/system']}>
          <SystemDashboardPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('SystemDashboardPage', () => {
  it('renders the real platform aggregate from the backend dashboard contract', async () => {
    systemApi.dashboard.mockResolvedValue({
      tenantsTotal: 3, tenantsActive: 2, tenantsSuspended: 1, tenantsArchived: 0,
      students: 5, teachers: 2, schoolAdmins: 2, plansTotal: 1, subscriptionsTotal: 1,
      aiUsageRecords: 4, aiTotalTokens: 100, supportOpen: 1, recentAuditEvents: 9, recentActivity: [],
    })
    renderPage()
    expect(await screen.findByRole('heading', { name: 'Platform administration' })).toBeInTheDocument()
    await waitFor(() => expect(systemApi.dashboard).toHaveBeenCalled())
  })

  it('surfaces an error/retry state when the dashboard read is denied', async () => {
    systemApi.dashboard.mockRejectedValue(new ApiError({ status: 403, title: 'Forbidden', detail: 'nope' }))
    renderPage()
    expect(await screen.findByText(/try again/i)).toBeInTheDocument()
  })
})
