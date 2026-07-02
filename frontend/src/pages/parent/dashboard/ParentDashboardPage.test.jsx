import { describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import ParentDashboardPage from './ParentDashboardPage'
import { parentApi } from '../../../features/parent/parentApi'

// Ported from the retired ParentPortalPage facade ("Phase 10 contracts"): the
// dashboard renders the real linked-children metric from the backend.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'parent-1', fullName: 'Parent One' } }),
}))

vi.mock('../../../features/parent/parentApi', () => ({
  parentApi: { dashboard: vi.fn(), children: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/parent']}>
          <ParentDashboardPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('ParentDashboardPage', () => {
  it('renders the linked-children dashboard metric from the backend', async () => {
    parentApi.dashboard.mockResolvedValue({ parentId: 'parent-1', linkedChildrenCount: 1 })
    parentApi.children.mockResolvedValue([])
    renderPage()
    expect(await screen.findByText('Parent dashboard')).toBeInTheDocument()
    expect(await screen.findByText('Linked children')).toBeInTheDocument()
    await waitFor(() => expect(parentApi.dashboard).toHaveBeenCalled())
  })
})
