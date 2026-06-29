import { describe, it, expect } from 'vitest'
import { render, screen, act } from '@testing-library/react'
import { ToastProvider, useToast } from './ToastProvider'

function Trigger({ onReady }) {
  const toast = useToast()
  onReady(toast)
  return null
}

function setup() {
  let toastApi
  render(
    <ToastProvider>
      <Trigger onReady={(t) => (toastApi = t)} />
    </ToastProvider>
  )
  return () => toastApi
}

describe('ToastProvider (notifications)', () => {
  it('renders an accessible live region', () => {
    setup()
    const region = document.querySelector('.ui-toast-region')
    expect(region).toHaveAttribute('aria-live', 'polite')
  })

  it('shows a success toast with status role', () => {
    const get = setup()
    act(() => {
      get().success('Saved')
    })
    expect(screen.getByText('Saved')).toBeInTheDocument()
    expect(screen.getByRole('status')).toBeInTheDocument()
  })

  it('uses role=alert for error toasts', () => {
    const get = setup()
    act(() => {
      get().error('Boom')
    })
    expect(screen.getByRole('alert')).toHaveTextContent('Boom')
  })

  it('deduplicates identical toasts fired in quick succession', () => {
    const get = setup()
    act(() => {
      get().info('Same')
      get().info('Same')
    })
    expect(screen.getAllByText('Same')).toHaveLength(1)
  })
})
