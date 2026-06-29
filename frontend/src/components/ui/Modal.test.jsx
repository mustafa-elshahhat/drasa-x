import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Modal } from './Modal'

describe('Modal (accessible dialog)', () => {
  it('renders as a labelled modal dialog', () => {
    render(
      <Modal open title="Confirm action" onClose={() => {}}>
        <button>Inside</button>
      </Modal>
    )
    const dialog = screen.getByRole('dialog')
    expect(dialog).toHaveAttribute('aria-modal', 'true')
    expect(dialog).toHaveAccessibleName('Confirm action')
  })

  it('closes on Escape', async () => {
    const onClose = vi.fn()
    const user = userEvent.setup()
    render(
      <Modal open title="T" onClose={onClose}>
        <button>Inside</button>
      </Modal>
    )
    await user.keyboard('{Escape}')
    expect(onClose).toHaveBeenCalled()
  })

  it('moves focus into the dialog on open', () => {
    render(
      <Modal open title="T" onClose={() => {}}>
        <button>First</button>
      </Modal>
    )
    // The first focusable (the header close button) receives focus.
    expect(screen.getByRole('dialog')).toContainElement(document.activeElement)
  })

  it('traps Tab focus within the dialog', async () => {
    const user = userEvent.setup()
    render(
      <Modal open title="T" onClose={() => {}}>
        <button>First</button>
        <button>Last</button>
      </Modal>
    )
    // From the last focusable, Tab wraps back to the first focusable (close btn),
    // staying within the dialog rather than escaping it.
    screen.getByRole('button', { name: 'Last' }).focus()
    await user.tab()
    expect(screen.getByRole('dialog')).toContainElement(document.activeElement)
    expect(screen.getByRole('button', { name: /close dialog/i })).toHaveFocus()
  })

  it('renders nothing when closed', () => {
    render(
      <Modal open={false} title="T" onClose={() => {}}>
        <button>Inside</button>
      </Modal>
    )
    expect(screen.queryByRole('dialog')).toBeNull()
  })
})
