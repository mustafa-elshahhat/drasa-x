import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '../../test/utils'
import ForgotPasswordPage from './ForgotPasswordPage'

// Task 3 (audit-driven fix pass): forgot-password must ALWAYS show the same
// generic success message, regardless of whether the login code belongs to a
// real account (the backend never discloses account existence — see
// AccountController.ForgotPassword, which always returns 200 with the same shape).

vi.mock('../../features/auth/authApi', () => ({
  forgotPassword: vi.fn(),
}))
import * as authApi from '../../features/auth/authApi'

beforeEach(() => vi.clearAllMocks())

describe('ForgotPasswordPage', () => {
  it('submits the login code and shows the generic success message', async () => {
    authApi.forgotPassword.mockResolvedValue({ ok: true, message: 'ok', devToken: null })
    const user = userEvent.setup()
    renderWithProviders(<ForgotPasswordPage />, { route: '/forgot-password' })

    await user.type(screen.getByLabelText(/Login code/i), 'stud-1')
    await user.click(screen.getByRole('button', { name: /Send reset instructions/i }))

    expect(
      await screen.findByText('If an account exists for that login code, you can now reset your password.'),
    ).toBeInTheDocument()
    expect(authApi.forgotPassword).toHaveBeenCalledWith('stud-1')
  })

  it('shows the identical generic message even for an unknown login code (no devToken)', async () => {
    authApi.forgotPassword.mockResolvedValue({ ok: true, message: 'ok' })
    const user = userEvent.setup()
    renderWithProviders(<ForgotPasswordPage />, { route: '/forgot-password' })

    await user.type(screen.getByLabelText(/Login code/i), 'unknown-code')
    await user.click(screen.getByRole('button', { name: /Send reset instructions/i }))

    expect(
      await screen.findByText('If an account exists for that login code, you can now reset your password.'),
    ).toBeInTheDocument()
    expect(screen.queryByText('Development helper')).not.toBeInTheDocument()
  })

  it('shows the dev-only token hint with a pre-filled reset link when devToken is present', async () => {
    authApi.forgotPassword.mockResolvedValue({ ok: true, message: 'ok', devToken: 'dev-token-123' })
    const user = userEvent.setup()
    renderWithProviders(<ForgotPasswordPage />, { route: '/forgot-password' })

    await user.type(screen.getByLabelText(/Login code/i), 'stud-1')
    await user.click(screen.getByRole('button', { name: /Send reset instructions/i }))

    expect(await screen.findByText('dev-token-123')).toBeInTheDocument()
    const link = screen.getByRole('link', { name: /Continue to reset password/i })
    expect(link).toHaveAttribute('href', '/reset-password?loginCode=stud-1&token=dev-token-123')
  })
})
