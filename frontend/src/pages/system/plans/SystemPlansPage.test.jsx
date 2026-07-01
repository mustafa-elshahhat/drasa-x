import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SystemPlansPage from './SystemPlansPage'
import { systemApi } from '../../../features/system/systemApi'

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'sys-1', fullName: 'Platform Admin' }, role: 'SystemAdmin' }),
}))

vi.mock('../../../features/system/systemApi', () => ({
  systemApi: {
    plans: vi.fn(),
    createPlan: vi.fn(),
    updatePlan: vi.fn(),
  },
}))

function renderPlans() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/system/plans']}>
          <SystemPlansPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('SystemPlansPage', () => {
  it('renders the Add Plan button and an empty state when there are no plans', async () => {
    systemApi.plans.mockResolvedValue([])
    renderPlans()
    expect(await screen.findByRole('button', { name: 'Add plan' })).toBeInTheDocument()
    expect(await screen.findByText('No plans defined yet.')).toBeInTheDocument()
  })

  it('opens the Add Plan modal when the button is clicked', async () => {
    systemApi.plans.mockResolvedValue([])
    const user = userEvent.setup()
    renderPlans()

    await user.click(await screen.findByRole('button', { name: 'Add plan' }))
    expect(await screen.findByRole('heading', { name: 'Add a subscription plan' })).toBeInTheDocument()
    expect(screen.getByLabelText('Plan name', { exact: false })).toBeInTheDocument()
    expect(screen.getByLabelText('Plan code', { exact: false })).toBeInTheDocument()
  })

  it('blocks submission and shows a validation error when required fields are empty', async () => {
    systemApi.plans.mockResolvedValue([])
    const user = userEvent.setup()
    renderPlans()

    await user.click(await screen.findByRole('button', { name: 'Add plan' }))
    await screen.findByRole('heading', { name: 'Add a subscription plan' })
    // Name/code are blank — client-side validation rejects before the API is called.
    await user.click(screen.getByRole('button', { name: 'Save' }))
    expect(await screen.findByText('Plan name is required.')).toBeInTheDocument()
    expect(systemApi.createPlan).not.toHaveBeenCalled()
  })

  it('creates a plan and refreshes the list on success', async () => {
    systemApi.plans.mockResolvedValueOnce([]).mockResolvedValueOnce([{ id: 'plan-new', code: 'PRO', name: 'Pro', tier: 1, billingPeriod: 0, price: 49, currency: 'USD', isActive: true }])
    systemApi.createPlan.mockResolvedValue({ id: 'plan-new', code: 'PRO', name: 'Pro' })
    const user = userEvent.setup()
    renderPlans()

    await user.click(await screen.findByRole('button', { name: 'Add plan' }))
    await screen.findByRole('heading', { name: 'Add a subscription plan' })

    await user.type(screen.getByLabelText('Plan name', { exact: false }), 'Pro')
    await user.type(screen.getByLabelText('Plan code', { exact: false }), 'PRO')
    await user.type(screen.getByLabelText('Price', { exact: false }), '49')
    await user.clear(screen.getByLabelText('Currency', { exact: false }))
    await user.type(screen.getByLabelText('Currency', { exact: false }), 'USD')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(systemApi.createPlan).toHaveBeenCalled())
    expect(systemApi.createPlan.mock.calls[0][0]).toMatchObject({ code: 'PRO', name: 'Pro', price: 49, currency: 'USD' })
    // "PRO" (the code cell) is unambiguous — "Pro" also appears as the tier chip label.
    expect(await screen.findByText('PRO')).toBeInTheDocument()
  })

  it('shows a server validation error without closing the modal', async () => {
    systemApi.plans.mockResolvedValue([])
    systemApi.createPlan.mockRejectedValue({ status: 409, title: 'Conflict', detail: 'A plan with this code already exists.' })
    const user = userEvent.setup()
    renderPlans()

    await user.click(await screen.findByRole('button', { name: 'Add plan' }))
    await screen.findByRole('heading', { name: 'Add a subscription plan' })
    await user.type(screen.getByLabelText('Plan name', { exact: false }), 'Pro')
    await user.type(screen.getByLabelText('Plan code', { exact: false }), 'PRO')
    await user.type(screen.getByLabelText('Price', { exact: false }), '49')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('A plan with this code already exists.')).toBeInTheDocument()
    // The modal stays open so the admin can correct the code.
    expect(screen.getByRole('heading', { name: 'Add a subscription plan' })).toBeInTheDocument()
  })
})
