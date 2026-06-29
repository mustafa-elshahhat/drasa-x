import { describe, it, expect, vi } from 'vitest'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '../../test/utils'
import { EmptyState, ErrorState } from './states'
import { ApiError } from '../../lib/api/problemDetails'

describe('Empty/Error states', () => {
  it('renders an empty state with role=status', () => {
    renderWithProviders(<EmptyState title="No items" message="Add one to begin" />)
    expect(screen.getByRole('status')).toHaveTextContent('No items')
  })

  it('renders a safe error message + correlation id, never a stack trace', () => {
    const err = new ApiError({ status: 500, title: 'X', correlationId: 'cid-42' })
    renderWithProviders(<ErrorState error={err} />)
    const alert = screen.getByRole('alert')
    expect(alert).toHaveTextContent(/server error/i)
    expect(alert).toHaveTextContent('cid-42')
  })

  it('invokes onRetry when the retry button is pressed', async () => {
    const onRetry = vi.fn()
    const user = userEvent.setup()
    renderWithProviders(<ErrorState error={new ApiError({ status: 0 })} onRetry={onRetry} />)
    await user.click(screen.getByRole('button'))
    expect(onRetry).toHaveBeenCalled()
  })
})
