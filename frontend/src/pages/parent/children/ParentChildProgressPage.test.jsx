import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import ParentChildProgressPage from './ParentChildProgressPage'
import { parentApi } from '../../../features/parent/parentApi'

// Regression test for §4 "Child progress (parent)" / P3-6: the page fetched
// `painPoints` via parentApi.childProgress() but silently dropped it — the
// approved-only, internal-free pain-point projection (StudentProgressService
// .PainPointsAsync, decision #7) was never rendered anywhere for parents.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'parent-1', fullName: 'Parent One' }, role: 'Parent' }),
}))

vi.mock('../../../features/parent/parentApi', () => ({
  parentApi: { childProgress: vi.fn() },
}))

function renderProgress() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/parent/children/child-1/progress']}>
          <Routes>
            <Route path="/app/parent/children/:childId/progress" element={<ParentChildProgressPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('ParentChildProgressPage pain points', () => {
  it('renders reviewed pain points returned by the API', async () => {
    parentApi.childProgress.mockResolvedValue({
      summary: { status: 'fulfilled', value: {} },
      subjects: { status: 'fulfilled', value: [] },
      attempts: { status: 'fulfilled', value: [] },
      insights: { status: 'fulfilled', value: [] },
      recommendations: { status: 'fulfilled', value: [] },
      painPoints: { status: 'fulfilled', value: [{ id: 'pp-1', topic: 'Fractions', severity: 'High' }] },
    })
    renderProgress()

    expect(await screen.findByText('Fractions')).toBeInTheDocument()
    expect(screen.getByText('Only teacher-reviewed items are shown here.')).toBeInTheDocument()
  })

  it('shows the empty state when there are no reviewed pain points yet', async () => {
    parentApi.childProgress.mockResolvedValue({
      summary: { status: 'fulfilled', value: {} },
      subjects: { status: 'fulfilled', value: [] },
      attempts: { status: 'fulfilled', value: [] },
      insights: { status: 'fulfilled', value: [] },
      recommendations: { status: 'fulfilled', value: [] },
      painPoints: { status: 'fulfilled', value: [] },
    })
    renderProgress()

    expect(await screen.findByText('No reviewed items yet.')).toBeInTheDocument()
  })
})
