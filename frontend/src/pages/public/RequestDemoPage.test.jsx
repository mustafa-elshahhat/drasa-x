import { describe, it, expect } from 'vitest'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '../../test/utils'
import RequestDemoPage from './RequestDemoPage'

// Regression test for D10 (audit-driven fix pass, P3-12): the form previously
// showed a fake "your request has been received" message with no backend and
// no data going anywhere. It now hands the visitor a real mailto: link (to
// the same address the public footer already advertises) instead of lying
// about automatic delivery.

describe('RequestDemoPage honest submission', () => {
  it('offers a real mailto link with the entered details instead of a fake "received" confirmation', async () => {
    const user = userEvent.setup()
    renderWithProviders(<RequestDemoPage />)

    await user.type(screen.getByLabelText(/first name/i), 'Ada')
    await user.type(screen.getByLabelText(/last name/i), 'Lovelace')
    await user.type(screen.getByLabelText(/^company/i), 'Analytical Engines Ltd')
    await user.type(screen.getByLabelText(/job title/i), 'Engineer')
    await user.type(screen.getByLabelText(/^city/i), 'Cairo')
    await user.type(screen.getByLabelText(/email address/i), 'ada@example.com')
    await user.click(screen.getByRole('button', { name: /request the demo/i }))

    const link = await screen.findByRole('link', { name: /send request by email/i })
    expect(link).toHaveAttribute('href', expect.stringContaining('mailto:info@derasax.com'))
    expect(link).toHaveAttribute('href', expect.stringContaining('Ada'))
    expect(link).toHaveAttribute('href', expect.stringContaining('Analytical%20Engines%20Ltd'))
    expect(screen.queryByText(/has been received/i)).not.toBeInTheDocument()
  })
})
