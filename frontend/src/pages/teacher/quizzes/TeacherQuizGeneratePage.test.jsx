import { describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import TeacherQuizGeneratePage from './TeacherQuizGeneratePage'
import { teacherApi } from '../../../features/teacher/teacherApi'

// Ported from the retired TeacherPortalPage facade ("Phase 9 contracts"): AI quiz
// generation is presented as draft-only and posts a draft request to the backend.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'teacher-1', fullName: 'Teacher One' } }),
}))

vi.mock('../../../features/teacher/teacherApi', () => ({
  teacherApi: { subjects: vi.fn(), generateDraft: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/teacher/quiz-generate']}>
          <TeacherQuizGeneratePage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('TeacherQuizGeneratePage', () => {
  it('is presented as draft-only and posts a draft request', async () => {
    teacherApi.subjects.mockResolvedValue([{ subjectId: 'PH8-SUBJECT-T1', name: 'Phase 8 Mathematics' }])
    teacherApi.generateDraft.mockResolvedValue({ quizId: 'NEW-DRAFT', status: 'AiGenerated' })
    const user = userEvent.setup()
    renderPage()
    expect(await screen.findByText('Draft-only generation')).toBeInTheDocument()
    await screen.findByText('Phase 8 Mathematics')
    await user.selectOptions(screen.getByLabelText('Subject'), 'PH8-SUBJECT-T1')
    await user.click(screen.getByRole('button', { name: /generate draft/i }))
    await waitFor(() => expect(teacherApi.generateDraft).toHaveBeenCalled())
    const arg = teacherApi.generateDraft.mock.calls[0][0]
    expect(arg.subjectId).toBe('PH8-SUBJECT-T1')
  })
})
