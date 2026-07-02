import { describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SystemOnboardingPage from './SystemOnboardingPage'
import { systemApi } from '../../../features/system/systemApi'

// Ported from the retired SystemAdminPortalPage facade ("Phase 12 contracts"): the
// onboarding wizard creates a tenant and its initial school admin through the
// real backend, and shows the one-time credential it returns.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'sys-1', fullName: 'Platform Admin' }, role: 'SystemAdmin' }),
}))

vi.mock('../../../features/system/systemApi', () => ({
  systemApi: { plans: vi.fn(), createTenant: vi.fn(), createSchoolAdmin: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/system/onboarding']}>
          <SystemOnboardingPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('SystemOnboardingPage', () => {
  it('onboards a tenant and creates its initial school admin through the backend', async () => {
    systemApi.plans.mockResolvedValue([{ id: 'plan-1', name: 'Pro' }])
    systemApi.createTenant.mockResolvedValue({ id: 'new-ten', name: 'New School' })
    systemApi.createSchoolAdmin.mockResolvedValue({ userId: 'u1', tenantId: 'new-ten', loginCode: 'NEW-ADM', role: 'SchoolAdmin', temporaryPassword: 'Temp123ABCdef' })
    const user = userEvent.setup()
    renderPage()

    await user.type(screen.getByLabelText('Tenant id'), 'new-ten')
    await user.type(screen.getByLabelText('Tenant name'), 'New School')
    await user.click(screen.getByRole('button', { name: 'Create tenant' }))
    await waitFor(() => expect(systemApi.createTenant).toHaveBeenCalled())
    expect(systemApi.createTenant.mock.calls[0][0]).toMatchObject({ id: 'new-ten', name: 'New School' })

    await user.type(screen.getByLabelText('Full name'), 'Founder Admin')
    await user.click(screen.getByRole('button', { name: 'Create school admin' }))
    await waitFor(() => expect(systemApi.createSchoolAdmin).toHaveBeenCalled())
    expect(systemApi.createSchoolAdmin.mock.calls[0][0]).toBe('new-ten')
    // No password/login-code is typed by the admin — both are generated server-side.
    expect(systemApi.createSchoolAdmin.mock.calls[0][1]).toEqual({ fullName: 'Founder Admin' })
    expect(await screen.findByText('NEW-ADM')).toBeInTheDocument() // one-time credential shown
  })

  it('does not render a password input anywhere on the page', async () => {
    systemApi.plans.mockResolvedValue([{ id: 'plan-1', name: 'Pro' }])
    renderPage()
    expect(screen.queryByLabelText(/password/i)).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Login code')).not.toBeInTheDocument()
  })

  it('rejects a non-English full name for the initial school admin', async () => {
    systemApi.plans.mockResolvedValue([{ id: 'plan-1', name: 'Pro' }])
    systemApi.createTenant.mockResolvedValue({ id: 'new-ten', name: 'New School' })
    const user = userEvent.setup()
    renderPage()

    await user.type(screen.getByLabelText('Tenant id'), 'new-ten')
    await user.type(screen.getByLabelText('Tenant name'), 'New School')
    await user.click(screen.getByRole('button', { name: 'Create tenant' }))
    await waitFor(() => expect(systemApi.createTenant).toHaveBeenCalled())

    await user.type(screen.getByLabelText('Full name'), 'محمد أحمد')
    expect(await screen.findByText('Full name must be written in English letters only.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Create school admin' })).toBeDisabled()
    expect(systemApi.createSchoolAdmin).not.toHaveBeenCalled()
  })
})
