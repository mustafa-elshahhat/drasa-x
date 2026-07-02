import { describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import ParentChildPointsPage from './ParentChildPointsPage'
import { parentApi } from '../../../features/parent/parentApi'

// Ported from the retired ParentPortalPage facade ("Phase 14 closure" contract):
// a parent sees a linked child's read-only gamification points and ledger.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'parent-1', fullName: 'Parent One' } }),
}))

vi.mock('../../../features/parent/parentApi', () => ({
  parentApi: { childPoints: vi.fn(), childPointsLedger: vi.fn() },
}))

function renderAt(childId) {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[`/app/parent/children/${childId}/points`]}>
          <Routes>
            <Route path="/app/parent/children/:childId/points" element={<ParentChildPointsPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('ParentChildPointsPage', () => {
  it("renders a linked child's read-only gamification points and ledger", async () => {
    parentApi.childPoints.mockResolvedValue({ studentId: 'stu-guid-1', totalPoints: 30 })
    parentApi.childPointsLedger.mockResolvedValue([{ id: 't1', points: 10, reason: 'Office hour' }])
    renderAt('stu-guid-1')
    expect(await screen.findByText('Gamification points')).toBeInTheDocument()
    expect(await screen.findByText('30')).toBeInTheDocument()
    await waitFor(() => expect(parentApi.childPoints).toHaveBeenCalledWith('stu-guid-1', expect.anything()))
    await waitFor(() => expect(parentApi.childPointsLedger).toHaveBeenCalledWith('stu-guid-1', expect.anything()))
  })
})
