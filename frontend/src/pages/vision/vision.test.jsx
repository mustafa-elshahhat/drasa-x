import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, within } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../i18n'
import en from '../../i18n/locales/en.js'
import ar from '../../i18n/locales/ar.js'
import { ApiError } from '../../lib/api/problemDetails'

// ---- mocks ----
vi.mock('../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'u1' }, status: 'authenticated', role: 'Teacher' }),
}))
vi.mock('../../features/vision/visionApi', () => ({
  visionApi: {
    listSessions: vi.fn(),
    startSession: vi.fn(),
    getSession: vi.fn(),
    endSession: vi.fn(),
    analyzeFrame: vi.fn(),
    listCandidates: vi.fn(),
    getSummary: vi.fn(),
    confirmCandidate: vi.fn(),
    rejectCandidate: vi.fn(),
    overrideCandidate: vi.fn(),
    myEngagementSummary: vi.fn(),
    childEngagementSummary: vi.fn(),
  },
  fileToBase64: vi.fn(async () => 'data:image/png;base64,AAAA'),
}))

import { visionApi } from '../../features/vision/visionApi'
import VisionStaffPage from './VisionStaffPage'
import StudentVisionPage from './StudentVisionPage'
import ParentVisionPage from './ParentVisionPage'

function renderAt(route, path, element) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={qc}>
        <MemoryRouter initialEntries={[route]}>
          <Routes>
            <Route path={path} element={element} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  i18n.changeLanguage('en')
})

describe('i18n vision keys (EN + AR parity)', () => {
  it('has the same vision keys in English and Arabic', () => {
    const enKeys = Object.keys(en.translation.vision).sort()
    const arKeys = Object.keys(ar.translation.vision).sort()
    expect(enKeys.length).toBeGreaterThan(10)
    expect(enKeys).toEqual(arKeys)
    expect(en.translation.vision.title).toBeTruthy()
    expect(ar.translation.vision.title).toBeTruthy()
  })
})

describe('VisionStaffPage — session list', () => {
  it('renders sessions and the start control', async () => {
    visionApi.listSessions.mockResolvedValue([
      { id: 's1', title: 'Grade 7', status: 'Active', degraded: true },
    ])
    renderAt('/app/vision', '/app/vision', <VisionStaffPage />)
    expect(await screen.findByText('Grade 7')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /start session/i })).toBeInTheDocument()
    // degraded sessions are flagged honestly
    expect(screen.getByText(/degraded/i)).toBeInTheDocument()
  })

  it('shows an empty state when there are no sessions', async () => {
    visionApi.listSessions.mockResolvedValue([])
    renderAt('/app/vision', '/app/vision', <VisionStaffPage />)
    expect(await screen.findByText(/no computer-vision sessions yet/i)).toBeInTheDocument()
  })
})

describe('VisionStaffPage — session detail', () => {
  beforeEach(() => {
    visionApi.getSession.mockResolvedValue({ id: 's1', title: 'Grade 7', status: 'Active', degraded: true, frameCount: 0 })
    visionApi.listCandidates.mockResolvedValue([])
    visionApi.getSummary.mockResolvedValue({ engagedObservations: 0, disengagedObservations: 0, notReadyObservations: 0 })
  })

  it('shows the degraded/test-engine banner honestly', async () => {
    renderAt('/app/vision/sessions/s1', '/app/vision/sessions/:sessionId', <VisionStaffPage />)
    expect(await screen.findByText(/degraded \/ test inference engine/i)).toBeInTheDocument()
  })

  it('shows an honest AI-unavailable state when analysis fails', async () => {
    visionApi.analyzeFrame.mockRejectedValue(new ApiError({ status: 502, title: 'AI unavailable', errorCode: 'AI_UNAVAILABLE' }))
    renderAt('/app/vision/sessions/s1', '/app/vision/sessions/:sessionId', <VisionStaffPage />)
    const input = await screen.findByTestId('cv-frame-input')
    const file = new File(['x'], 'frame.png', { type: 'image/png' })
    fireEvent.change(input, { target: { files: [file] } })
    expect(await screen.findByText(/ai service unavailable/i)).toBeInTheDocument()
    // never shows a fabricated success
    expect(screen.queryByTestId('cv-detections')).not.toBeInTheDocument()
  })

  it('renders detected faces after a successful analysis', async () => {
    visionApi.analyzeFrame.mockResolvedValue({
      facesDetected: 1, engine: 'stub', degraded: true,
      results: [{ trackId: 'ext-A', recognitionStatus: 'candidate', recognitionConfidence: 0.8, emotion: 'Happy', engagement: 'NotReady' }],
    })
    visionApi.listCandidates.mockResolvedValue([
      { id: 'c1', trackId: 'ext-A', recognitionStatus: 'candidate', bestRecognitionConfidence: 0.8, reviewStatus: 'Pending', mappedStudentId: null },
    ])
    renderAt('/app/vision/sessions/s1', '/app/vision/sessions/:sessionId', <VisionStaffPage />)
    const input = await screen.findByTestId('cv-frame-input')
    fireEvent.change(input, { target: { files: [new File(['x'], 'f.png', { type: 'image/png' })] } })
    const detections = await screen.findByTestId('cv-detections')
    expect(within(detections).getByText(/ext-A/)).toBeInTheDocument()
  })
})

describe('StudentVisionPage — own summary only', () => {
  it('renders the student summary', async () => {
    visionApi.myEngagementSummary.mockResolvedValue({ engagedObservations: 3, disengagedObservations: 1, notReadyObservations: 2, cvAttendanceCount: 1, sessionsObserved: 1 })
    renderAt('/app/student/vision', '/app/student/vision', <StudentVisionPage />)
    expect(await screen.findByTestId('cv-student-summary')).toBeInTheDocument()
    expect(screen.getByText(/my engagement/i)).toBeInTheDocument()
  })

  it('shows an empty state when there is no CV data', async () => {
    visionApi.myEngagementSummary.mockResolvedValue({ engagedObservations: 0, disengagedObservations: 0, notReadyObservations: 0, cvAttendanceCount: 0 })
    renderAt('/app/student/vision', '/app/student/vision', <StudentVisionPage />)
    expect(await screen.findByText(/no engagement data yet/i)).toBeInTheDocument()
  })
})

describe('ParentVisionPage — linked child only', () => {
  it('shows an honest error for an unlinked/forbidden child', async () => {
    visionApi.childEngagementSummary.mockRejectedValue(new ApiError({ status: 403, title: 'Forbidden', errorCode: 'FORBIDDEN' }))
    renderAt('/app/parent/children/other/vision', '/app/parent/children/:childId/vision', <ParentVisionPage />)
    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(screen.queryByTestId('cv-student-summary')).not.toBeInTheDocument()
  })

  it('renders the linked child summary', async () => {
    visionApi.childEngagementSummary.mockResolvedValue({ engagedObservations: 2, disengagedObservations: 0, notReadyObservations: 0, cvAttendanceCount: 1, sessionsObserved: 1 })
    renderAt('/app/parent/children/c1/vision', '/app/parent/children/:childId/vision', <ParentVisionPage />)
    expect(await screen.findByTestId('cv-student-summary')).toBeInTheDocument()
  })
})
