import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SystemTenantsPage from './SystemTenantsPage'
import { systemApi } from '../../../features/system/systemApi'

// Regression test for F-01 / P0-1a: the tenants list's "Open" row action was
// silently dropped by ResourceTable and never rendered.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'sys-1', fullName: 'Platform Admin' }, role: 'SystemAdmin' }),
}))

vi.mock('../../../features/system/systemApi', () => ({
  systemApi: { tenants: vi.fn() },
}))

function renderTenants() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/system/tenants']}>
          <SystemTenantsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('SystemTenantsPage row actions', () => {
  it('renders an Open link per tenant row that targets the tenant detail route', async () => {
    systemApi.tenants.mockResolvedValue([
      { id: 'tenant-1', name: 'Riyadh Academy', status: 0 },
      { id: 'tenant-2', name: 'Jeddah School', status: 0 },
    ])
    renderTenants()

    const links = await screen.findAllByRole('link', { name: 'Open' })
    expect(links).toHaveLength(2)
    expect(links[0]).toHaveAttribute('href', '/app/system/tenants/tenant-1')
    expect(links[1]).toHaveAttribute('href', '/app/system/tenants/tenant-2')
  })
})

// Ported from the retired SystemAdminPortalPage facade ("Phase 12 contracts"):
// tenants returned by the backend render in the list (and the API is actually called).
describe('SystemTenantsPage backend listing', () => {
  it('lists tenants returned by the backend', async () => {
    systemApi.tenants.mockResolvedValue([{ id: 'tenant-1', name: 'Main School', status: 0 }])
    renderTenants()
    expect(await screen.findByText(/Main School/)).toBeInTheDocument()
    await waitFor(() => expect(systemApi.tenants).toHaveBeenCalled())
  })
})
