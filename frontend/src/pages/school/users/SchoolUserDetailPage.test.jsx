import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import { ApiError } from '../../../lib/api/problemDetails'
import SchoolUserDetailPage from './SchoolUserDetailPage'
import { schoolApi } from '../../../features/school/schoolApi'

// Regression test for §6 row 1 / P1-1 / D9: SchoolAdmin user lists had zero
// row actions and no detail page — enable/disable/reset-credential were dead
// API methods never wired to any UI.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: { getUser: vi.fn(), setUserEnabled: vi.fn(), resetUserCredential: vi.fn() },
}))

function renderDetail(userId = 'student-1') {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[`/app/school/students/${userId}`]}>
          <Routes>
            <Route path="/app/school/students/:userId" element={<SchoolUserDetailPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('SchoolUserDetailPage', () => {
  it('renders the fetched user and lets an admin disable an enabled account', async () => {
    schoolApi.getUser.mockResolvedValue({ id: 'student-1', fullName: 'Sara Ali', role: 'Student', loginCode: 'sara01', isDisabled: false })
    schoolApi.setUserEnabled.mockResolvedValue({ id: 'student-1', isDisabled: true })
    const user = userEvent.setup()
    renderDetail()

    expect(await screen.findByRole('heading', { name: 'Sara Ali' })).toBeInTheDocument()
    const disableBtn = screen.getByRole('button', { name: 'Disable account' })
    expect(disableBtn).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Enable account' })).toBeDisabled()

    await user.click(disableBtn)
    await waitFor(() => expect(schoolApi.setUserEnabled).toHaveBeenCalledWith('student-1', false))
  })

  it('resets credentials and displays the new one-time password', async () => {
    schoolApi.getUser.mockResolvedValue({ id: 'student-1', fullName: 'Sara Ali', role: 'Student', loginCode: 'sara01', isDisabled: false })
    schoolApi.resetUserCredential.mockResolvedValue({ loginCode: 'sara01', temporaryPassword: 'Tmp$3cret99' })
    const user = userEvent.setup()
    renderDetail()

    await screen.findByRole('heading', { name: 'Sara Ali' })
    await user.click(screen.getByRole('button', { name: 'Reset credential' }))

    await waitFor(() => expect(schoolApi.resetUserCredential).toHaveBeenCalledWith('student-1'))
    expect(await screen.findByText('Tmp$3cret99')).toBeInTheDocument()
  })

  it('shows an error state when the user cannot be loaded', async () => {
    schoolApi.getUser.mockRejectedValue(new ApiError({ status: 404, title: 'Not found', detail: 'User not found.' }))
    renderDetail('missing-user')
    expect(await screen.findByText('The requested item could not be found.')).toBeInTheDocument()
  })
})
