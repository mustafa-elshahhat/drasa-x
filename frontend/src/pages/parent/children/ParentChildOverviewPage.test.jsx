import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import ParentChildOverviewPage from './ParentChildOverviewPage'
import { parentApi } from '../../../features/parent/parentApi'
import { ApiError } from '../../../lib/api/problemDetails'

// Regression test for §4 "Child vision (parent)" / P0-6 / P2-3: the overview
// only linked to progress/attendance/points — the registered child-vision
// route had no entry point anywhere in the UI.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'parent-1', fullName: 'Parent One' }, role: 'Parent' }),
}))

vi.mock('../../../features/parent/parentApi', () => ({
  parentApi: { child: vi.fn() },
}))

function renderOverview(path = '/app/parent/children/child-1') {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/app/parent/children/:childId" element={<ParentChildOverviewPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('ParentChildOverviewPage vision link', () => {
  it('links to the child vision route', async () => {
    parentApi.child.mockResolvedValue({ id: 'child-1', fullName: 'Sara', summary: {} })
    renderOverview()

    const link = await screen.findByRole('link', { name: /engagement/i })
    expect(link).toHaveAttribute('href', '/app/parent/children/child-1/vision')
  })
})

// Ported from the retired ParentPortalPage facade ("Phase 10 contracts"): a
// denied child read (not your linked child) surfaces an honest error/retry state.
describe('ParentChildOverviewPage error state', () => {
  it('surfaces a forbidden/error state when a child read is denied', async () => {
    parentApi.child.mockRejectedValue(new ApiError({ status: 403, title: 'Forbidden', detail: 'not your child' }))
    renderOverview('/app/parent/children/other-child')
    expect(await screen.findByText(/try again/i)).toBeInTheDocument()
  })
})
