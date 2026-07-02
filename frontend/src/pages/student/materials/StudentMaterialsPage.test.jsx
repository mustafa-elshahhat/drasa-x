import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import { ToastProvider } from '../../../components/feedback/ToastProvider'
import StudentMaterialsPage from './StudentMaterialsPage'
import { studentApi } from '../../../features/student/studentApi'
import { filesApi } from '../../../features/files/filesApi'

// P1-6 — StudentMaterialsPage previously never fetched the material itself (decorative thumb +
// comments only; audit §4/§9 "placeholder detail"). These tests prove the real title/type render
// and the download/legacy-link action is driven by real fetched data.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'student-1', fullName: 'Sara' }, role: 'Student' }),
}))

vi.mock('../../../features/student/studentApi', () => ({
  studentApi: {
    material: vi.fn(),
    resourceComments: vi.fn(),
    addResourceComment: vi.fn(),
  },
}))

vi.mock('../../../features/files/filesApi', () => ({
  filesApi: { download: vi.fn() },
}))

function renderAt(path) {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <ToastProvider>
          <MemoryRouter initialEntries={[path]}>
            <Routes>
              <Route path="/app/student/materials/:materialId" element={<StudentMaterialsPage />} />
            </Routes>
          </MemoryRouter>
        </ToastProvider>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  studentApi.resourceComments.mockResolvedValue([])
})

describe('StudentMaterialsPage — real material fetch (P1-6)', () => {
  it('fetches and renders the real material title and type', async () => {
    studentApi.material.mockResolvedValue({
      id: 'mat-1', title: 'Week 1 Slides', type: 4, lessonId: 'lesson-1', fileRecordId: null, url: null,
    })
    renderAt('/app/student/materials/mat-1')

    expect(await screen.findByText('Week 1 Slides')).toBeInTheDocument()
    expect(await screen.findByText('Slides')).toBeInTheDocument()
    expect(studentApi.material).toHaveBeenCalledWith('mat-1', expect.anything())
  })

  it('renders a real Download action and calls filesApi.download when fileRecordId is present', async () => {
    studentApi.material.mockResolvedValue({
      id: 'mat-2', title: 'Recorded lecture', type: 1, lessonId: 'lesson-1', fileRecordId: 'file-9', url: '/api/v1/files/file-9/download',
    })
    const user = userEvent.setup()
    renderAt('/app/student/materials/mat-2')

    const button = await screen.findByRole('button', { name: 'Download' })
    await user.click(button)
    expect(filesApi.download).toHaveBeenCalledWith('file-9', 'Recorded lecture')
  })

  it('renders a real external link (not a decorative thumb-only view) when only url is present', async () => {
    studentApi.material.mockResolvedValue({
      id: 'mat-3', title: 'Reading list', type: 2, lessonId: 'lesson-1', fileRecordId: null, url: 'https://example.com/reading',
    })
    renderAt('/app/student/materials/mat-3')

    const link = await screen.findByRole('link', { name: 'Open link' })
    expect(link).toHaveAttribute('href', 'https://example.com/reading')
    expect(screen.queryByRole('button', { name: 'Download' })).not.toBeInTheDocument()
  })

  it('shows an honest not-found state when the fetch resolves with no material', async () => {
    studentApi.material.mockResolvedValue(null)
    renderAt('/app/student/materials/mat-missing')

    expect(await screen.findByText('Material not found')).toBeInTheDocument()
  })
})
