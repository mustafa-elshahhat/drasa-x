import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { I18nextProvider } from 'react-i18next'
import i18n from '../i18n'
import { Breadcrumbs } from './Breadcrumbs'

// Regression test for F-05 / P1-8: intermediate breadcrumb crumbs whose
// cumulative path isn't itself a registered route (only its `:param` variant
// is) were rendered as a Link anyway, navigating to a 404 on click.

function renderAt(path) {
  return render(
    <I18nextProvider i18n={i18n}>
      <MemoryRouter initialEntries={[path]}>
        <Breadcrumbs />
      </MemoryRouter>
    </I18nextProvider>,
  )
}

describe('Breadcrumbs', () => {
  it('renders a Link for a crumb whose cumulative path is registered', () => {
    // /app/teacher/units/unit-1 -> crumbs: /app, /app/teacher, /app/teacher/units, /app/teacher/units/unit-1
    // /app/teacher IS registered exactly.
    renderAt('/app/teacher/units/unit-1')
    const teacherCrumb = screen.getByRole('link', { name: 'Teacher dashboard' })
    expect(teacherCrumb).toHaveAttribute('href', '/app/teacher')
  })

  it('renders plain text (not a Link) for a crumb with no registered route of its own', () => {
    // The bare /app/teacher/units has no registered route — only /app/teacher/units/:unitId does.
    renderAt('/app/teacher/units/unit-1')
    expect(screen.queryByRole('link', { name: 'units' })).not.toBeInTheDocument()
    expect(screen.getByText('units')).toBeInTheDocument()
  })

  it('still renders a Link for an intermediate crumb that matches a registered :param route', () => {
    // /app/teacher/classes/class-1 -> the intermediate-turned-registered case: since
    // /app/teacher/classes IS itself registered (list + detail share the family),
    // it must remain a Link.
    renderAt('/app/teacher/classes/class-1')
    expect(screen.getByRole('link', { name: 'My classes' })).toHaveAttribute('href', '/app/teacher/classes')
  })

  it('never renders the current (last) crumb as a Link', () => {
    renderAt('/app/teacher/units/unit-1')
    const current = screen.getByText('unit-1')
    expect(current.tagName).not.toBe('A')
    expect(current).toHaveAttribute('aria-current', 'page')
  })
})
