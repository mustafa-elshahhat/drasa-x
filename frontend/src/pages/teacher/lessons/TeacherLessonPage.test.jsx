import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import { ToastProvider } from '../../../components/feedback/ToastProvider'
import TeacherLessonPage from './TeacherLessonPage'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { filesApi } from '../../../features/files/filesApi'

// P1-4 — curriculum authoring UI: TeacherLessonPage was materials-display-only, with no way to
// upload/add a material and no open/download action on existing rows (audit §6, §9). These tests
// prove the upload form calls teacherApi.uploadMaterial, the link form calls teacherApi.addMaterial,
// and rows expose a real download (uploaded file) or link (legacy URL) action.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'teach-1', fullName: 'Teacher One' }, role: 'Teacher' }),
}))

vi.mock('../../../features/teacher/teacherApi', () => ({
  teacherApi: {
    lessonMaterials: vi.fn(),
    uploadMaterial: vi.fn(),
    addMaterial: vi.fn(),
    deleteMaterial: vi.fn(),
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
              <Route path="/app/teacher/lessons/:lessonId" element={<TeacherLessonPage />} />
            </Routes>
          </MemoryRouter>
        </ToastProvider>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  teacherApi.lessonMaterials.mockResolvedValue([])
  teacherApi.uploadMaterial.mockResolvedValue({ id: 'mat-new', title: 'Week 1 slides' })
  teacherApi.addMaterial.mockResolvedValue({ id: 'mat-link', title: 'External reading' })
  teacherApi.deleteMaterial.mockResolvedValue(true)
})

describe('TeacherLessonPage — material upload/add/download', () => {
  it('uploads a material file and calls teacherApi.uploadMaterial with the lesson id, title, and type', async () => {
    const user = userEvent.setup()
    renderAt('/app/teacher/lessons/lesson-1')

    await screen.findByRole('heading', { name: 'Add lesson material' })
    await user.type(screen.getByLabelText('Material title', { exact: false }), 'Week 1 slides')

    const file = new File(['hello'], 'slides.pdf', { type: 'application/pdf' })
    const fileInput = screen.getByLabelText('Upload lesson material', { exact: false })
    await user.upload(fileInput, file)
    await user.click(screen.getByRole('button', { name: 'Upload' }))

    await waitFor(() => expect(teacherApi.uploadMaterial).toHaveBeenCalled())
    const call = teacherApi.uploadMaterial.mock.calls[0][0]
    expect(call).toMatchObject({ lessonId: 'lesson-1', title: 'Week 1 slides', type: 2 })
    expect(call.file).toBe(file)
  })

  it('adds a URL-only material and calls teacherApi.addMaterial', async () => {
    const user = userEvent.setup()
    renderAt('/app/teacher/lessons/lesson-1')

    await screen.findByRole('heading', { name: 'Add lesson material' })
    await user.type(screen.getByLabelText('Material title', { exact: false }), 'External reading')
    await user.type(screen.getByLabelText('External link (URL)', { exact: false }), 'https://example.com/reading')
    await user.click(screen.getByRole('button', { name: 'Add link' }))

    await waitFor(() =>
      expect(teacherApi.addMaterial).toHaveBeenCalledWith({
        lessonId: 'lesson-1',
        title: 'External reading',
        url: 'https://example.com/reading',
        type: 2,
      }),
    )
  })

  it('shows a Download action for an uploaded (fileRecordId) material and calls filesApi.download', async () => {
    teacherApi.lessonMaterials.mockResolvedValue([
      { id: 'mat-1', title: 'Slides deck', lessonId: 'lesson-1', type: 4, fileRecordId: 'file-1', url: '/api/v1/files/file-1/download' },
    ])
    const user = userEvent.setup()
    renderAt('/app/teacher/lessons/lesson-1')

    await user.click(await screen.findByRole('button', { name: 'Download' }))
    expect(filesApi.download).toHaveBeenCalledWith('file-1', 'Slides deck')
  })

  it('shows a real link (not a download button) for a legacy URL-only material', async () => {
    teacherApi.lessonMaterials.mockResolvedValue([
      { id: 'mat-2', title: 'Reading list', lessonId: 'lesson-1', type: 2, fileRecordId: null, url: 'https://example.com/list' },
    ])
    renderAt('/app/teacher/lessons/lesson-1')

    const link = await screen.findByRole('link', { name: 'Open link' })
    expect(link).toHaveAttribute('href', 'https://example.com/list')
    expect(screen.queryByRole('button', { name: 'Download' })).not.toBeInTheDocument()
  })

  it('deletes a material and calls teacherApi.deleteMaterial', async () => {
    teacherApi.lessonMaterials.mockResolvedValue([
      { id: 'mat-3', title: 'Old handout', lessonId: 'lesson-1', type: 2, fileRecordId: null, url: 'https://example.com/handout' },
    ])
    const user = userEvent.setup()
    renderAt('/app/teacher/lessons/lesson-1')

    await user.click(await screen.findByRole('button', { name: 'Delete' }))
    await waitFor(() => expect(teacherApi.deleteMaterial).toHaveBeenCalledWith('mat-3'))
  })
})
