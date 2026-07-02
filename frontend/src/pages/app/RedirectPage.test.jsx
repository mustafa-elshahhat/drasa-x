import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route, useLocation } from 'react-router-dom'
import RedirectPage from './RedirectPage'

// Regression test for D3/D4 (audit-driven fix pass, P3-10/P3-11): duplicate
// route aliases now redirect to their canonical URL instead of silently
// rendering the same page under two different paths.

function LocationProbe() {
  const location = useLocation()
  return <div data-testid="probe">{location.pathname}</div>
}

function renderAt(initialPath, fromPath, to) {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route path={fromPath} element={<RedirectPage to={to} />} />
        <Route path="*" element={<LocationProbe />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('RedirectPage', () => {
  it('redirects a static path with no params', () => {
    renderAt('/app/student/streaks', '/app/student/streaks', '/app/student/badges')
    expect(screen.getByTestId('probe')).toHaveTextContent('/app/student/badges')
  })

  it('substitutes a single :param from the matched route into the target template', () => {
    renderAt('/app/student/leaderboard/comp-42', '/app/student/leaderboard/:competitionId', '/app/student/competitions/:competitionId')
    expect(screen.getByTestId('probe')).toHaveTextContent('/app/student/competitions/comp-42')
  })

  it('URL-encodes a substituted param value', () => {
    renderAt('/app/student/leaderboard/a b', '/app/student/leaderboard/:competitionId', '/app/student/competitions/:competitionId')
    expect(screen.getByTestId('probe')).toHaveTextContent('/app/student/competitions/a%20b')
  })
})
