import { describe, it, expect } from 'vitest'
import { axe } from 'jest-axe'
import { renderWithProviders } from './utils'
import { Modal } from '../components/ui/Modal'
import { EmptyState } from '../components/ui/states'
import { TextField, PasswordField } from '../components/form/fields'
import { Button } from '../components/ui/Button'

// Automated accessibility checks (Phase 7 §15) using axe-core. These catch a
// class of violations (missing labels, ARIA misuse, contrast in some cases);
// manual keyboard/screen-reader checks are documented in ACCESSIBILITY.md.
describe('accessibility (axe)', () => {
  it('accessible form controls have no detectable a11y violations', async () => {
    const { container } = renderWithProviders(
      <form aria-label="Example form">
        <TextField label="Login code" />
        <PasswordField label="Password" />
        <Button type="submit">Submit</Button>
      </form>
    )
    expect(await axe(container)).toHaveNoViolations()
  })

  it('modal dialog has no detectable a11y violations', async () => {
    const { container } = renderWithProviders(
      <Modal open title="Example" onClose={() => {}}>
        <p>Body content</p>
      </Modal>
    )
    expect(await axe(container)).toHaveNoViolations()
  })

  it('empty state has no detectable a11y violations', async () => {
    const { container } = renderWithProviders(<EmptyState title="Nothing" message="Add one" />)
    expect(await axe(container)).toHaveNoViolations()
  })
})
