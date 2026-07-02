import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '../../test/utils'
import { ApiError } from '../../lib/api/problemDetails'
import ResetPasswordPage from './ResetPasswordPage'

// Task 3 (audit-driven fix pass): reset-password submits the 3 real backend
// fields (LoginCode, Token, NewPassword) and surfaces the real 400 "Invalid or
// expired reset token." message via the existing ErrorState pattern.

vi.mock('../../features/auth/authApi', () => ({
  resetPassword: vi.fn(),
}))
import * as authApi from '../../features/auth/authApi'

beforeEach(() => vi.clearAllMocks())

describe('ResetPasswordPage', () => {
  it('submits all 3 fields and shows success', async () => {
    authApi.resetPassword.mockResolvedValue({ ok: true, message: 'ok' })
    const user = userEvent.setup()
    renderWithProviders(<ResetPasswordPage />, { route: '/reset-password' })

    await user.type(screen.getByLabelText(/Login code/i), 'stud-1')
    await user.type(screen.getByLabelText(/Reset token/i), 'tok-1')
    await user.type(screen.getByLabelText(/^New password/i), 'newpassword1')
    await user.type(screen.getByLabelText(/Confirm new password/i), 'newpassword1')
    await user.click(screen.getByRole('button', { name: /^Reset password$/i }))

    expect(await screen.findByText('Your password has been reset. You can now sign in.')).toBeInTheDocument()
    expect(authApi.resetPassword).toHaveBeenCalledWith({ loginCode: 'stud-1', token: 'tok-1', newPassword: 'newpassword1' })
  })

  it('shows a client-side error when the new passwords do not match (no backend call)', async () => {
    const user = userEvent.setup()
    renderWithProviders(<ResetPasswordPage />, { route: '/reset-password' })

    await user.type(screen.getByLabelText(/Login code/i), 'stud-1')
    await user.type(screen.getByLabelText(/Reset token/i), 'tok-1')
    await user.type(screen.getByLabelText(/^New password/i), 'newpassword1')
    await user.type(screen.getByLabelText(/Confirm new password/i), 'different1')
    await user.click(screen.getByRole('button', { name: /^Reset password$/i }))

    expect(await screen.findByText('The new passwords do not match.')).toBeInTheDocument()
    expect(authApi.resetPassword).not.toHaveBeenCalled()
  })

  it('shows an error state on a mocked 400 (invalid or expired token)', async () => {
    authApi.resetPassword.mockResolvedValue({
      ok: false,
      error: new ApiError({ status: 400, title: 'Invalid or expired reset token.' }),
    })
    const user = userEvent.setup()
    renderWithProviders(<ResetPasswordPage />, { route: '/reset-password' })

    await user.type(screen.getByLabelText(/Login code/i), 'stud-1')
    await user.type(screen.getByLabelText(/Reset token/i), 'bad-token')
    await user.type(screen.getByLabelText(/^New password/i), 'newpassword1')
    await user.type(screen.getByLabelText(/Confirm new password/i), 'newpassword1')
    await user.click(screen.getByRole('button', { name: /^Reset password$/i }))

    expect(await screen.findByText('Invalid or expired reset token.')).toBeInTheDocument()
  })

  it('pre-fills the login code and token from the query string (dev-link convenience)', async () => {
    renderWithProviders(<ResetPasswordPage />, { route: '/reset-password?loginCode=stud-1&token=dev-tok' })

    expect(await screen.findByLabelText(/Login code/i)).toHaveValue('stud-1')
    expect(screen.getByLabelText(/Reset token/i)).toHaveValue('dev-tok')
  })
})
