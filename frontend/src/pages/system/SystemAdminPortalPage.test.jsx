import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../i18n'
import { createQueryClient } from '../../lib/query/queryClient'
import SystemAdminPortalPage from './SystemAdminPortalPage'
import { systemApi } from '../../features/system/systemApi'
import { ApiError } from '../../lib/api/problemDetails'

vi.mock('../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'sys-1', fullName: 'Platform Admin' }, role: 'SystemAdmin' }),
}))

vi.mock('../../features/system/systemApi', () => ({
  systemApi: {
    dashboard: vi.fn(), tenants: vi.fn(), tenant: vi.fn(), tenantSubscription: vi.fn(), tenantUsage: vi.fn(),
    createTenant: vi.fn(), setTenantStatus: vi.fn(), plans: vi.fn(), assignPlan: vi.fn(), subscriptions: vi.fn(),
    createSchoolAdmin: vi.fn(), usage: vi.fn(), aiUsage: vi.fn(), storage: vi.fn(),
    supportTickets: vi.fn(), respondSupportTicket: vi.fn(), announcements: vi.fn(), createAnnouncement: vi.fn(),
    featureFlags: vi.fn(), upsertFeatureFlag: vi.fn(), settings: vi.fn(), upsertSetting: vi.fn(),
    audit: vi.fn(), securityEvents: vi.fn(), operationalStatus: vi.fn(),
    exportTenantData: vi.fn(), requestTenantDeletion: vi.fn(),
  },
}))

function renderSystem(view, { path = '/app/system', entries = ['/app/system'] } = {}) {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={entries}>
          <Routes>
            <Route path={path} element={<SystemAdminPortalPage view={view} />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('SystemAdminPortalPage Phase 12 contracts', () => {
  it('renders the real platform aggregate from the backend dashboard contract', async () => {
    systemApi.dashboard.mockResolvedValue({
      tenantsTotal: 3, tenantsActive: 2, tenantsSuspended: 1, tenantsArchived: 0,
      students: 5, teachers: 2, schoolAdmins: 2, plansTotal: 1, subscriptionsTotal: 1,
      aiUsageRecords: 4, aiTotalTokens: 100, supportOpen: 1, recentAuditEvents: 9, recentActivity: [],
    })
    renderSystem('dashboard')
    expect(await screen.findByRole('heading', { name: 'Platform administration' })).toBeInTheDocument()
    await waitFor(() => expect(systemApi.dashboard).toHaveBeenCalled())
  })

  it('lists tenants returned by the backend', async () => {
    systemApi.tenants.mockResolvedValue([{ id: 'tenant-1', name: 'Main School', status: 0 }])
    renderSystem('tenants')
    expect(await screen.findByText(/Main School/)).toBeInTheDocument()
    await waitFor(() => expect(systemApi.tenants).toHaveBeenCalled())
  })

  it('shows an empty state when there are no support tickets', async () => {
    systemApi.supportTickets.mockResolvedValue([])
    renderSystem('support')
    expect(await screen.findByText('No support tickets yet.')).toBeInTheDocument()
  })

  it('renders Phase 19 observability fields on the operational status page', async () => {
    systemApi.operationalStatus.mockResolvedValue({
      health: { api: 'up', databaseReachable: true, databaseNote: 'ok' },
      storage: { configured: true, status: 'healthy', note: 'Local storage root is accessible.' },
      aiService: { configured: false, status: 'degraded', note: 'AI service unreachable.' },
      backgroundJobs: { configured: true, status: 'healthy', note: 'disabled' },
      metrics: { totalRequests: 42, status2xx: 40, status3xx: 0, status4xx: 2, status5xx: 0, avgLatencyMs: 12.3 },
      version: '1.2.3', environment: 'Production', uptimeSeconds: 123.4,
    })
    renderSystem('health')
    expect(await screen.findByText('File storage')).toBeInTheDocument()
    expect(screen.getByText('AI service')).toBeInTheDocument()
    expect(screen.getByText('Background jobs')).toBeInTheDocument()
    expect(screen.getByText('Request metrics')).toBeInTheDocument()
    expect(screen.getByText('Deployment')).toBeInTheDocument()
    await waitFor(() => expect(systemApi.operationalStatus).toHaveBeenCalled())
  })

  it('surfaces an error/retry state when the dashboard read is denied', async () => {
    systemApi.dashboard.mockRejectedValue(new ApiError({ status: 403, title: 'Forbidden', detail: 'nope' }))
    renderSystem('dashboard')
    expect(await screen.findByText(/try again/i)).toBeInTheDocument()
  })

  it('reports honest storage posture (byte accounting not implemented)', async () => {
    systemApi.storage.mockResolvedValue({ byteAccountingImplemented: false, note: 'Byte usage is not measured yet.', tenants: [] })
    renderSystem('storage')
    expect(await screen.findByText(/Byte usage is not measured yet\./)).toBeInTheDocument()
  })

  it('onboards a tenant and creates its initial school admin through the backend', async () => {
    systemApi.plans.mockResolvedValue([{ id: 'plan-1', name: 'Pro' }])
    systemApi.createTenant.mockResolvedValue({ id: 'new-ten', name: 'New School' })
    systemApi.createSchoolAdmin.mockResolvedValue({ userId: 'u1', tenantId: 'new-ten', loginCode: 'NEW-ADM', role: 'SchoolAdmin', temporaryPassword: 'Temp123ABCdef' })
    const user = userEvent.setup()
    renderSystem('onboarding')

    await user.type(screen.getByLabelText('Tenant id'), 'new-ten')
    await user.type(screen.getByLabelText('Tenant name'), 'New School')
    await user.click(screen.getByRole('button', { name: 'Create tenant' }))
    await waitFor(() => expect(systemApi.createTenant).toHaveBeenCalled())
    expect(systemApi.createTenant.mock.calls[0][0]).toMatchObject({ id: 'new-ten', name: 'New School' })

    await user.type(screen.getByLabelText('Full name'), 'Founder Admin')
    await user.type(screen.getByLabelText('Login code'), 'NEW-ADM')
    await user.click(screen.getByRole('button', { name: 'Create school admin' }))
    await waitFor(() => expect(systemApi.createSchoolAdmin).toHaveBeenCalled())
    expect(systemApi.createSchoolAdmin.mock.calls[0][0]).toBe('new-ten')
    expect(systemApi.createSchoolAdmin.mock.calls[0][1]).toMatchObject({ fullName: 'Founder Admin', loginCode: 'NEW-ADM' })
    expect(await screen.findByText('NEW-ADM')).toBeInTheDocument() // one-time credential shown
  })

  it('suspends a tenant from the tenant-details lifecycle controls', async () => {
    systemApi.tenant.mockResolvedValue({ id: 'tenant-1', name: 'Main School', status: 0 })
    systemApi.tenantSubscription.mockResolvedValue(null)
    systemApi.tenantUsage.mockResolvedValue({ studentsCount: 3 })
    systemApi.setTenantStatus.mockResolvedValue({ id: 'tenant-1', status: 1 })
    const user = userEvent.setup()
    renderSystem('tenant-details', { path: '/app/system/tenants/:tenantId', entries: ['/app/system/tenants/tenant-1'] })

    const suspendBtn = await screen.findByRole('button', { name: 'Suspend' })
    await user.click(suspendBtn)
    await waitFor(() => expect(systemApi.setTenantStatus).toHaveBeenCalledWith('tenant-1', 'suspend'))
  })
})
