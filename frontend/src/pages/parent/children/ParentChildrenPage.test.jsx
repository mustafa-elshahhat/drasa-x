import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import ParentChildrenPage from './ParentChildrenPage'
import { parentApi } from '../../../features/parent/parentApi'

// Ported from the retired ParentPortalPage facade ("Phase 10 contracts"): the
// children list is scoped to the backend's linked children, with an honest
// empty state when the parent has none linked.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'parent-1', fullName: 'Parent One' } }),
}))

vi.mock('../../../features/parent/parentApi', () => ({
  parentApi: { children: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/parent/children']}>
          <ParentChildrenPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('ParentChildrenPage', () => {
  it('lists only the linked children returned by the backend', async () => {
    parentApi.children.mockResolvedValue([{ studentId: 'stu-guid-1', fullName: 'Tenant1 Student', className: 'Phase 8 Class' }])
    renderPage()
    expect(await screen.findByText(/Tenant1 Student/)).toBeInTheDocument()
  })

  it('shows an empty state when the parent has no linked children', async () => {
    parentApi.children.mockResolvedValue([])
    renderPage()
    expect(await screen.findByText('No children are linked to your account.')).toBeInTheDocument()
  })
})
