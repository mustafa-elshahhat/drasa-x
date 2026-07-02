import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../i18n'
import { ResourceTable } from './ResourceTable'

// Regression test for the F-01 audit finding: `rowActions` was accepted by page
// callers (via features/school|system/components.jsx `List`) but silently
// dropped by ResourceTable, so no row action ever rendered (tenants "Open"
// link, feature-flag toggle, relationship deactivate, announcement publish).

function renderTable(props) {
  return render(
    <I18nextProvider i18n={i18n}>
      <ResourceTable
        rows={[{ id: '1', name: 'Row one' }, { id: '2', name: 'Row two' }]}
        columns={[{ key: 'name', header: 'Name' }]}
        {...props}
      />
    </I18nextProvider>,
  )
}

describe('ResourceTable rowActions', () => {
  it('renders a trailing actions cell per row when rowActions is provided', () => {
    renderTable({ rowActions: (row) => <button>{`Open ${row.name}`}</button> })

    expect(screen.getByRole('button', { name: 'Open Row one' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Open Row two' })).toBeInTheDocument()
  })

  it('renders no actions column when rowActions is omitted (unchanged default behavior)', () => {
    renderTable({})
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('still works when columns is omitted and auto-derived', () => {
    render(
      <I18nextProvider i18n={i18n}>
        <ResourceTable
          rows={[{ id: '1', label: 'Auto row' }]}
          rowActions={(row) => <button>{`Act on ${row.label}`}</button>}
        />
      </I18nextProvider>,
    )
    expect(screen.getByRole('button', { name: 'Act on Auto row' })).toBeInTheDocument()
  })
})
