import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import StudentSubjectsPage from './StudentSubjectsPage'
import { studentApi } from '../../../features/student/studentApi'

// Ported from the retired StudentPortalPage facade ("Phase 8 contracts"): the
// subjects list renders backend data and filters client-side by search text.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'student-1', fullName: 'Sara' }, role: 'Student' }),
}))

vi.mock('../../../features/student/studentApi', () => ({
  studentApi: { subjects: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/student/subjects']}>
          <StudentSubjectsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('StudentSubjectsPage', () => {
  it('renders subjects page with search and filters, and filters client-side', async () => {
    studentApi.subjects.mockResolvedValue([
      { id: 'math', name: 'Mathematics', teacher: 'Mr. Osama Refat', units: 6, progress: 62 },
      { id: 'phys', name: 'Physics', teacher: 'Dr. Ahmed Hassan', units: 5, progress: 48 },
    ])

    renderPage()

    // Expect subjects to render
    expect(await screen.findByText('Mathematics')).toBeInTheDocument()
    expect(screen.getByText('Physics')).toBeInTheDocument()

    // Test search filter
    const searchInput = screen.getByPlaceholderText(/search/i)
    await userEvent.type(searchInput, 'Math')
    expect(screen.getByText('Mathematics')).toBeInTheDocument()
    expect(screen.queryByText('Physics')).not.toBeInTheDocument()
  })
})
