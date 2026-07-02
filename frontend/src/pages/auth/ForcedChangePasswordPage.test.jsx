import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../i18n'
import ForcedChangePasswordPage from './ForcedChangePasswordPage'

// The forced change-password page (reachable only via RequirePasswordChange once
// mustChangePassword is true) must: reject a same-as-current new password client-side,
// call changePassword() on a valid submit and land on the user's home route, and offer a
// logout escape hatch.

const changePassword = vi.fn()
const logout = vi.fn()

vi.mock('../../features/auth/AuthContext', () => ({
  useAuth: () => ({ changePassword, logout, role: 'Student' }),
}))

function renderPage(entries = ['/change-password']) {
  return render(
    <I18nextProvider i18n={i18n}>
      <MemoryRouter initialEntries={entries}>
        <Routes>
          <Route path="/change-password" element={<ForcedChangePasswordPage />} />
          <Route path="/app/student" element={<div>STUDENT HOME</div>} />
          <Route path="/login" element={<div>LOGIN PAGE</div>} />
        </Routes>
      </MemoryRouter>
    </I18nextProvider>
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('ForcedChangePasswordPage', () => {
  it('changes the password and lands on the user home route', async () => {
    changePassword.mockResolvedValue({ message: 'Password changed.' })
    const user = userEvent.setup()
    renderPage()

    await user.type(screen.getByLabelText(/Current password/i), 'Temp#12345678')
    await user.type(screen.getByLabelText(/^New password/i), 'Fresh#Pass9000')
    await user.type(screen.getByLabelText(/Confirm new password/i), 'Fresh#Pass9000')
    await user.click(screen.getByRole('button', { name: /Change password and continue/i }))

    expect(await screen.findByText('STUDENT HOME')).toBeInTheDocument()
    expect(changePassword).toHaveBeenCalledWith({ currentPassword: 'Temp#12345678', newPassword: 'Fresh#Pass9000' })
  })

  it('rejects a new password equal to the current password (client-side, no call)', async () => {
    const user = userEvent.setup()
    renderPage()

    await user.type(screen.getByLabelText(/Current password/i), 'Temp#12345678')
    await user.type(screen.getByLabelText(/^New password/i), 'Temp#12345678')
    await user.type(screen.getByLabelText(/Confirm new password/i), 'Temp#12345678')
    await user.click(screen.getByRole('button', { name: /Change password and continue/i }))

    expect(await screen.findByText('Your new password must be different from the temporary password.')).toBeInTheDocument()
    expect(changePassword).not.toHaveBeenCalled()
  })

  it('logs out via the escape hatch without changing the password', async () => {
    logout.mockResolvedValue(undefined)
    const user = userEvent.setup()
    renderPage()

    await user.click(screen.getByRole('button', { name: /Sign out/i }))
    expect(await screen.findByText('LOGIN PAGE')).toBeInTheDocument()
    expect(logout).toHaveBeenCalled()
  })
})
