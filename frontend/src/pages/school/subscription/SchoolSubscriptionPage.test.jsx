import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SchoolSubscriptionPage from './SchoolSubscriptionPage'
import { schoolApi } from '../../../features/school/schoolApi'

// Regression test: the subscription page was pure read-only — there was no way
// for a school admin to request a renewal from the UI.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: { subscription: vi.fn(), requestRenewal: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school/subscription']}>
          <SchoolSubscriptionPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  schoolApi.subscription.mockResolvedValue({ planName: 'Pro', status: 'Active' })
})

describe('SchoolSubscriptionPage renewal request', () => {
  it('submits a renewal request with notes and no expiry date', async () => {
    schoolApi.requestRenewal.mockResolvedValue({ id: 'ren-1', status: 0 })
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Pro')
    await user.type(screen.getByLabelText('Notes'), 'Please extend by a year')
    await user.click(screen.getByRole('button', { name: 'Request renewal' }))

    await waitFor(() =>
      expect(schoolApi.requestRenewal).toHaveBeenCalledWith({ requestedExpiresAt: null, notes: 'Please extend by a year' }),
    )
    expect(await screen.findByText('Renewal requested')).toBeInTheDocument()
  })

  it('submits a renewal request with a requested expiry date', async () => {
    schoolApi.requestRenewal.mockResolvedValue({ id: 'ren-2', status: 0 })
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Pro')
    fireEvent.change(screen.getByLabelText('Requested expiry date'), { target: { value: '2027-06-30' } })
    await user.click(screen.getByRole('button', { name: 'Request renewal' }))

    await waitFor(() =>
      expect(schoolApi.requestRenewal).toHaveBeenCalledWith({ requestedExpiresAt: '2027-06-30', notes: null }),
    )
  })
})
