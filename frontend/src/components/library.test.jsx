import { describe, it, expect, vi } from 'vitest'
import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '../test/utils'

import { Chip } from './ui/Chip'
import { NotEnoughData } from './ui/NotEnoughData'
import { Tabs } from './ui/Tabs'
import { Toggle } from './ui/Toggle'
import { Ring } from './viz/Ring'
import { ProgressBar } from './viz/ProgressBar'
import { Bars } from './viz/Bars'
import { Heatmap } from './viz/Heatmap'
import { ResourceTable } from './data/ResourceTable'
import { DetailList } from './data/DetailList'
import { Crud } from './data/Crud'
import { OptionCard } from './quiz/OptionCard'
import { LeaderboardRow } from './domain/LeaderboardRow'

describe('Chip', () => {
  it('applies the tone class and renders children', () => {
    renderWithProviders(<Chip tone="success">Active</Chip>)
    const chip = screen.getByText('Active')
    expect(chip).toHaveClass('ui-chip', 'ui-chip--success')
  })
})

describe('NotEnoughData (honesty state)', () => {
  it('renders a status with the not-enough fallback text and never a number', () => {
    renderWithProviders(<NotEnoughData />)
    const node = screen.getByRole('status')
    expect(node.textContent).toMatch(/not enough data/i)
  })
})

describe('viz honest fallbacks', () => {
  it('Ring with no value shows not-enough-data, not a 0% reading', () => {
    renderWithProviders(<Ring value={undefined} />)
    expect(screen.getByRole('status').textContent).toMatch(/not enough data/i)
    expect(screen.queryByText('0%')).toBeNull()
  })

  it('Ring with a real value renders the percentage', () => {
    renderWithProviders(<Ring value={75} />)
    expect(screen.getByText('75%')).toBeInTheDocument()
  })

  it('ProgressBar with no value reads as — rather than 0%', () => {
    renderWithProviders(<ProgressBar value={null} label="Progress" />)
    expect(screen.getByText('—')).toBeInTheDocument()
  })

  it('Bars with no finite points shows not-enough-data', () => {
    renderWithProviders(<Bars series={[{ label: 'A', value: null }]} />)
    expect(screen.getByRole('status').textContent).toMatch(/not enough data/i)
  })

  it('Heatmap with no cells shows not-enough-data', () => {
    renderWithProviders(<Heatmap cells={[]} />)
    expect(screen.getByRole('status').textContent).toMatch(/not enough data/i)
  })
})

describe('ResourceTable (typed columns)', () => {
  const columns = [
    { key: 'name', header: 'Name' },
    { key: 'status', header: 'Status', chip: () => ({ tone: 'success', label: 'Open' }) },
  ]

  it('renders translated headers and chip-rendered cells (not raw keys)', () => {
    renderWithProviders(
      <ResourceTable rows={[{ name: 'Algebra', status: 1 }]} columns={columns} />
    )
    expect(screen.getByRole('columnheader', { name: 'Name' })).toBeInTheDocument()
    expect(screen.getByText('Algebra')).toBeInTheDocument()
    expect(screen.getByText('Open')).toHaveClass('ui-chip--success')
  })

  it('shows a skeleton while loading and an empty state when there are no rows', () => {
    const { rerender } = renderWithProviders(<ResourceTable rows={[]} columns={columns} loading />)
    expect(screen.getByRole('status')).toBeInTheDocument() // loading announcement
    rerender(<ResourceTable rows={[]} columns={columns} emptyTitle="Nothing" />)
    expect(screen.getByText('Nothing')).toBeInTheDocument()
  })
})

describe('DetailList (dual-case, translated labels)', () => {
  it('reads PascalCase data with a camelCase key config and shows the label', () => {
    const { container } = renderWithProviders(
      <DetailList item={{ StartDate: '2026-01-01T00:00:00Z' }} fields={[{ key: 'startDate', label: 'Start', format: 'date' }]} />
    )
    expect(screen.getByText('Start')).toBeInTheDocument()
    // value resolved via dual-case read + formatted (not the em-dash placeholder,
    // not the raw camelCase key). Digits are locale-dependent so we don't pin them.
    const dd = container.querySelector('.ui-detail-list__row dd')
    expect(dd.textContent).not.toBe('—')
    expect(dd.textContent).not.toContain('startDate')
    expect(screen.queryByText('startDate')).toBeNull()
  })
})

describe('Crud', () => {
  it('opens a create form modal when the add button is clicked', async () => {
    const user = userEvent.setup()
    renderWithProviders(
      <Crud
        title="Plans"
        rows={[]}
        columns={[{ key: 'name', header: 'Name' }]}
        createLabel="Add plan"
        createTitle="New plan"
        createFields={[{ name: 'name', label: 'Name' }]}
        onCreate={vi.fn()}
      />
    )
    await user.click(screen.getByRole('button', { name: /add plan/i }))
    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(within(screen.getByRole('dialog')).getByText('New plan')).toBeInTheDocument()
  })
})

describe('Tabs', () => {
  it('marks the active tab and fires onChange', async () => {
    const onChange = vi.fn()
    const user = userEvent.setup()
    renderWithProviders(
      <Tabs tabs={[{ id: 'a', label: 'All' }, { id: 'b', label: 'Open' }]} active="a" onChange={onChange} />
    )
    expect(screen.getByRole('tab', { name: 'All' })).toHaveAttribute('aria-selected', 'true')
    await user.click(screen.getByRole('tab', { name: 'Open' }))
    expect(onChange).toHaveBeenCalledWith('b')
  })
})

describe('Toggle', () => {
  it('is a switch and toggles its value', async () => {
    const onChange = vi.fn()
    const user = userEvent.setup()
    renderWithProviders(<Toggle checked={false} onChange={onChange} label="AI tutor" />)
    const sw = screen.getByRole('switch', { name: 'AI tutor' })
    expect(sw).toHaveAttribute('aria-checked', 'false')
    await user.click(sw)
    expect(onChange).toHaveBeenCalledWith(true)
  })
})

describe('OptionCard', () => {
  it('renders a radio option bound to a question field', () => {
    renderWithProviders(<OptionCard name="q:1" value="opt-1" label="Choice A" marker="A" />)
    const radio = screen.getByRole('radio')
    expect(radio).toHaveAttribute('name', 'q:1')
    expect(radio).toHaveAttribute('value', 'opt-1')
    expect(screen.getByText('Choice A')).toBeInTheDocument()
  })
})

describe('LeaderboardRow', () => {
  it('renders rank, name and points and medal-styles the top three', () => {
    const { container } = renderWithProviders(
      <ul><LeaderboardRow rank={1} name="Sara" points={120} /></ul>
    )
    expect(screen.getByText('Sara')).toBeInTheDocument()
    expect(screen.getByText('120')).toBeInTheDocument()
    expect(container.querySelector('.domain-lb__rank.is-medal-1')).toBeTruthy()
  })
})
