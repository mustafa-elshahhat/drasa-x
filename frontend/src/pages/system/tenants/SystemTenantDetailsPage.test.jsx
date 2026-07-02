import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SystemTenantDetailsPage from './SystemTenantDetailsPage'
import { systemApi } from '../../../features/system/systemApi'

// Regression test for §6 row 13 "Renewal processing; tenant archive button" /
// P2-9: systemApi.js:53-56's setTenantStatus already supported an 'archive'
// action but no button ever called it.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'sys-1', fullName: 'Platform Admin' }, role: 'SystemAdmin' }),
}))

vi.mock('../../../features/system/systemApi', () => ({
  systemApi: {
    tenant: vi.fn(), tenantSubscription: vi.fn(), tenantUsage: vi.fn(), plans: vi.fn(),
    setTenantStatus: vi.fn(), assignPlan: vi.fn(), createSchoolAdmin: vi.fn(), exportTenantData: vi.fn(), requestTenantDeletion: vi.fn(),
    processRenewal: vi.fn(),
  },
}))

function renderDetail() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/system/tenants/tenant-1']}>
          <Routes>
            <Route path="/app/system/tenants/:tenantId" element={<SystemTenantDetailsPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  systemApi.tenantSubscription.mockResolvedValue(null)
  systemApi.tenantUsage.mockResolvedValue(null)
  systemApi.plans.mockResolvedValue([])
})

describe('SystemTenantDetailsPage archive', () => {
  it('archives an active tenant via the lifecycle Archive button', async () => {
    systemApi.tenant.mockResolvedValue({ id: 'tenant-1', name: 'Riyadh Academy', status: 0 })
    systemApi.setTenantStatus.mockResolvedValue({})
    const user = userEvent.setup()
    renderDetail()

    const archiveBtn = await screen.findByRole('button', { name: 'Archive' })
    expect(archiveBtn).toBeEnabled()
    await user.click(archiveBtn)

    await waitFor(() => expect(systemApi.setTenantStatus).toHaveBeenCalledWith('tenant-1', 'archive'))
  })

  it('disables Archive when the tenant is already archived', async () => {
    systemApi.tenant.mockResolvedValue({ id: 'tenant-1', name: 'Riyadh Academy', status: 2 })
    renderDetail()

    const archiveBtn = await screen.findByRole('button', { name: 'Archive' })
    expect(archiveBtn).toBeDisabled()
  })
})

// Regression test for Task 1 (audit-driven fix pass): the system-admin side of
// subscription-renewal processing had no UI at all. No "list pending renewals for
// tenant X" backend endpoint exists (verified against TenantsController and
// ITenantAdminService), so the honest minimal UI is an id-entry form against the
// real POST /api/v1/tenants/renewals/{renewalId}/process contract.
describe('SystemTenantDetailsPage renewal processing', () => {
  it('disables the process-renewal button until an id and a decision are entered', async () => {
    systemApi.tenant.mockResolvedValue({ id: 'tenant-1', name: 'Riyadh Academy', status: 0 })
    renderDetail()

    const processBtn = await screen.findByRole('button', { name: 'Process renewal request' })
    expect(processBtn).toBeDisabled()
  })

  it('processes a renewal via the id-entry form, calling processRenewal with the right args', async () => {
    systemApi.tenant.mockResolvedValue({ id: 'tenant-1', name: 'Riyadh Academy', status: 0 })
    systemApi.processRenewal.mockResolvedValue({ id: 'renewal-1', status: 1 })
    const user = userEvent.setup()
    renderDetail()

    await screen.findByRole('button', { name: 'Archive' })
    await user.type(screen.getByLabelText('Renewal request id'), 'renewal-1')
    await user.selectOptions(screen.getByLabelText('Decision'), '1')

    const processBtn = screen.getByRole('button', { name: 'Process renewal request' })
    expect(processBtn).toBeEnabled()
    await user.click(processBtn)

    await waitFor(() => expect(systemApi.processRenewal).toHaveBeenCalledWith('renewal-1', {
      status: 1,
      newExpiresAt: undefined,
      notes: undefined,
    }))
  })
})

// Ported from the retired SystemAdminPortalPage facade ("Phase 12 contracts"): the
// Suspend lifecycle control calls the real backend command.
describe('SystemTenantDetailsPage suspend', () => {
  it('suspends a tenant from the tenant-details lifecycle controls', async () => {
    systemApi.tenant.mockResolvedValue({ id: 'tenant-1', name: 'Main School', status: 0 })
    systemApi.tenantSubscription.mockResolvedValue(null)
    systemApi.tenantUsage.mockResolvedValue({ studentsCount: 3 })
    systemApi.setTenantStatus.mockResolvedValue({ id: 'tenant-1', status: 1 })
    const user = userEvent.setup()
    renderDetail()

    const suspendBtn = await screen.findByRole('button', { name: 'Suspend' })
    await user.click(suspendBtn)
    await waitFor(() => expect(systemApi.setTenantStatus).toHaveBeenCalledWith('tenant-1', 'suspend'))
  })
})
