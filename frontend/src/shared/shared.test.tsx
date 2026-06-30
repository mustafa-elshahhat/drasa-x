import { describe, it, expect } from 'vitest'
import { screen } from '@testing-library/react'
import { renderWithProviders } from '../test/utils'
import { Badge, SectionHeader } from './ui'
import { PageShell } from './layout'
import { StatGrid, MetricCard } from './data-display'
import { LoadingState, QueryState, type QueryLike } from './feedback'

// Phase 3 — smoke coverage for the shared UI foundation. Also exercises the TS
// test path (a .tsx test compiled + run by Vitest) so ts/tsx is covered end to end.
describe('shared UI foundation', () => {
  it('Badge (Chip alias) renders its children with a tone class', () => {
    const { container } = renderWithProviders(<Badge tone="success">Active</Badge>)
    expect(screen.getByText('Active')).toBeTruthy()
    expect(container.querySelector('.ui-chip--success')).toBeTruthy()
  })

  it('SectionHeader renders its title and actions', () => {
    renderWithProviders(<SectionHeader title="Subjects" actions={<button>Add</button>} />)
    expect(screen.getByRole('heading', { name: 'Subjects' })).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Add' })).toBeTruthy()
  })

  it('StatGrid renders metric cards from real values', () => {
    renderWithProviders(
      <StatGrid>
        <MetricCard label="Points" value={120} />
      </StatGrid>
    )
    expect(screen.getByText('Points')).toBeTruthy()
    expect(screen.getByText('120')).toBeTruthy()
  })

  it('LoadingState shows its label', () => {
    renderWithProviders(<LoadingState label="Loading subjects" />)
    expect(screen.getByText('Loading subjects')).toBeTruthy()
  })

  it('QueryState renders data via render-prop and an error state on failure', () => {
    const ok: QueryLike<string[]> = {
      isLoading: false,
      isError: false,
      data: ['a', 'b'],
      refetch: () => {},
    }
    const { unmount } = renderWithProviders(
      <QueryState query={ok}>{(data: string[]) => <span>{`count:${data.length}`}</span>}</QueryState>
    )
    expect(screen.getByText('count:2')).toBeTruthy()
    unmount()

    const bad: QueryLike<string[]> = {
      isLoading: false,
      isError: true,
      error: { title: 'Boom' },
      data: undefined,
      refetch: () => {},
    }
    renderWithProviders(<QueryState query={bad}>{() => <span>never</span>}</QueryState>)
    expect(screen.getByRole('alert')).toBeTruthy()
  })

  it('PageShell renders a page header and content', () => {
    renderWithProviders(
      <PageShell title="Dashboard">
        <p>body</p>
      </PageShell>
    )
    expect(screen.getByRole('heading', { name: 'Dashboard' })).toBeTruthy()
    expect(screen.getByText('body')).toBeTruthy()
  })
})
