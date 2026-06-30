import { describe, it, expect } from 'vitest'
import { screen, fireEvent } from '@testing-library/react'
import { renderWithProviders } from '../test/utils'
import { Button, Card, PageHeader, Dialog, Drawer } from './ui'
import { EmptyState, ErrorState } from './feedback'
import { DataTable } from './data-display'

// =============================================================================
// Phase 13.6 — shared UI smoke coverage, exercised THROUGH the `src/shared`
// facade (the public import surface pages now use). Complements shared.test.tsx
// (Badge / SectionHeader / StatGrid / LoadingState / QueryState / PageShell).
// =============================================================================

describe('shared/ui primitives (via facade)', () => {
  it('Button renders the requested variant class and children', () => {
    const { container, rerender } = renderWithProviders(<Button variant="primary">Save</Button>)
    expect(screen.getByRole('button', { name: 'Save' })).toBeTruthy()
    expect(container.querySelector('.ui-btn--primary')).toBeTruthy()

    rerender(<Button variant="danger">Delete</Button>)
    expect(container.querySelector('.ui-btn--danger')).toBeTruthy()
  })

  it('Button loading state disables the button and marks it busy', () => {
    renderWithProviders(<Button loading>Submit</Button>)
    const btn = screen.getByRole('button', { name: 'Submit' }) as HTMLButtonElement
    expect(btn.disabled).toBe(true)
    expect(btn.getAttribute('aria-busy')).toBe('true')
  })

  it('Card renders title, description and children', () => {
    renderWithProviders(
      <Card title="Subjects" description="Your enrolled subjects">
        <p>body content</p>
      </Card>
    )
    expect(screen.getByRole('heading', { name: 'Subjects' })).toBeTruthy()
    expect(screen.getByText('Your enrolled subjects')).toBeTruthy()
    expect(screen.getByText('body content')).toBeTruthy()
  })

  it('PageHeader renders title, description and an actions slot', () => {
    renderWithProviders(
      <PageHeader title="Dashboard" description="Overview" actions={<button>New</button>} />
    )
    expect(screen.getByRole('heading', { name: 'Dashboard' })).toBeTruthy()
    expect(screen.getByText('Overview')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'New' })).toBeTruthy()
  })
})

describe('shared/feedback states (via facade)', () => {
  it('EmptyState renders an honest empty title + message (role=status)', () => {
    renderWithProviders(<EmptyState title="No subjects yet" message="Enroll to begin" />)
    const region = screen.getByRole('status')
    expect(region).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'No subjects yet' })).toBeTruthy()
    expect(screen.getByText('Enroll to begin')).toBeTruthy()
  })

  it('ErrorState renders role=alert and a retry button that fires onRetry', () => {
    let retried = 0
    renderWithProviders(<ErrorState error={{ title: 'Boom' }} onRetry={() => (retried += 1)} />)
    expect(screen.getByRole('alert')).toBeTruthy()
    fireEvent.click(screen.getByRole('button'))
    expect(retried).toBe(1)
  })
})

describe('shared/data-display DataTable (via facade)', () => {
  const columns = [
    { key: 'name', header: 'Name' },
    { key: 'score', header: 'Score' },
  ]

  it('renders an empty state when there are no rows', () => {
    renderWithProviders(
      <DataTable columns={columns} rows={[]} emptyTitle="No students" emptyMessage="None enrolled" />
    )
    expect(screen.getByRole('status')).toBeTruthy()
    expect(screen.getByText('No students')).toBeTruthy()
  })

  it('renders a busy table while loading', () => {
    const { container } = renderWithProviders(<DataTable columns={columns} rows={null} loading />)
    expect(container.querySelector('[aria-busy="true"]')).toBeTruthy()
  })

  it('renders an error state (role=alert) and hides table data', () => {
    renderWithProviders(<DataTable columns={columns} rows={null} error={{ title: 'Nope' }} />)
    expect(screen.getByRole('alert')).toBeTruthy()
    expect(document.querySelector('table')).toBeNull()
  })

  it('renders rows when data is present', () => {
    renderWithProviders(
      <DataTable
        columns={columns}
        rows={[{ id: 1, name: 'Ada', score: 99 }]}
        rowKey={(r) => r.id}
      />
    )
    expect(screen.getByText('Ada')).toBeTruthy()
    expect(screen.getByText('99')).toBeTruthy()
  })
})

describe('shared overlays (Dialog / Drawer smoke)', () => {
  it('Dialog renders title + children when open and nothing when closed', () => {
    const { rerender, queryByText } = renderWithProviders(
      <Dialog open title="Confirm">
        <p>dialog body</p>
      </Dialog>
    )
    expect(screen.getByText('dialog body')).toBeTruthy()
    rerender(
      <Dialog open={false} title="Confirm">
        <p>dialog body</p>
      </Dialog>
    )
    expect(queryByText('dialog body')).toBeNull()
  })

  it('Drawer renders as a dialog when open and closes via Escape', () => {
    let closed = 0
    renderWithProviders(
      <Drawer open title="Filters" onClose={() => (closed += 1)}>
        <p>drawer body</p>
      </Drawer>
    )
    expect(screen.getByRole('dialog', { name: 'Filters' })).toBeTruthy()
    expect(screen.getByText('drawer body')).toBeTruthy()
    fireEvent.keyDown(document, { key: 'Escape' })
    expect(closed).toBe(1)
  })
})

describe('shared RTL smoke', () => {
  it('a representative shared layout component renders under dir=rtl', () => {
    document.documentElement.setAttribute('dir', 'rtl')
    try {
      renderWithProviders(
        <Drawer open title="القائمة" side="end">
          <p>محتوى</p>
        </Drawer>
      )
      // The drawer uses logical edges (start/end); under RTL it still mounts and
      // exposes its dialog + content (the physical flip is CSS, asserted visually).
      expect(screen.getByRole('dialog', { name: 'القائمة' })).toBeTruthy()
      expect(screen.getByText('محتوى')).toBeTruthy()
    } finally {
      document.documentElement.removeAttribute('dir')
    }
  })
})
