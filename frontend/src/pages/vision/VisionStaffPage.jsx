import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Camera } from 'lucide-react'
import { PageHeader, Card } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState } from '../../components/ui/states'
import { Button } from '../../components/ui/Button'
import { Alert } from '../../components/ui/Alert'
import { useAuth } from '../../features/auth/AuthContext'
import { visionApi, fileToBase64 } from '../../features/vision/visionApi'
import { queryKeys, STALE } from '../../lib/query/keys'

// =============================================================================
// Phase 15 — Teacher / School-Admin computer-vision workspace.
// List + start CV sessions, analyze frames (backend-mediated AI), review
// detected attendance candidates (confirm/reject/override), and read the
// engagement summary. The browser never calls the AI service directly, and the
// degraded/test-engine state is surfaced honestly.
// =============================================================================

function DegradedBanner() {
  const { t } = useTranslation()
  return (
    <Alert variant="warning" title={t('vision.degradedTitle')}>
      {t('vision.degradedBody')}
    </Alert>
  )
}

export default function VisionStaffPage() {
  const { sessionId } = useParams()
  return sessionId ? <SessionDetail sessionId={sessionId} /> : <SessionList />
}

// ---------------------------------------------------------------------------
// List + start
// ---------------------------------------------------------------------------
function SessionList() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [title, setTitle] = useState('')

  const sessions = useQuery({
    queryKey: queryKeys.vision.sessions(user?.id),
    queryFn: ({ signal }) => visionApi.listSessions({}, signal),
    staleTime: STALE.short,
  })

  const start = useMutation({
    mutationFn: () => visionApi.startSession({ title: title || undefined }),
    onSuccess: (session) => {
      qc.invalidateQueries({ queryKey: queryKeys.vision.sessions(user?.id) })
      if (session?.id) navigate(`/app/vision/sessions/${session.id}`)
    },
  })

  return (
    <>
      <PageHeader title={t('vision.title')} description={t('vision.description')} />
      <p className="ui-muted">{t('vision.privacyNote')}</p>

      <Card title={t('vision.startTitle')}>
        <form
          className="ui-form-row"
          onSubmit={(e) => {
            e.preventDefault()
            start.mutate()
          }}
        >
          <label className="ui-field">
            <span>{t('vision.sessionTitle')}</span>
            <input
              className="ui-input"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder={t('vision.sessionTitlePlaceholder')}
              aria-label={t('vision.sessionTitle')}
            />
          </label>
          <Button type="submit" loading={start.isPending}>
            <Camera size={16} aria-hidden="true" /> {t('vision.start')}
          </Button>
        </form>
        {start.isError && <ErrorState error={start.error} />}
      </Card>

      <Card title={t('vision.sessionsTitle')}>
        {sessions.isLoading && <p role="status">{t('states.loading', 'Loading…')}</p>}
        {sessions.isError && <ErrorState error={sessions.error} onRetry={sessions.refetch} />}
        {sessions.data && sessions.data.length === 0 && <EmptyState title={t('vision.noSessions')} />}
        {sessions.data && sessions.data.length > 0 && (
          <ul className="ui-list" data-testid="cv-session-list">
            {sessions.data.map((s) => (
              <li key={s.id} className="ui-list__item">
                <Link to={`/app/vision/sessions/${s.id}`} className="cv-session-link">
                  <strong>{s.title || t('vision.untitled')}</strong>
                  <span className={`ui-badge ui-badge--${s.status === 'Active' ? 'success' : 'muted'}`}>
                    {t(`vision.status_${(s.status || '').toLowerCase()}`, s.status)}
                  </span>
                  {s.degraded && <span className="ui-badge ui-badge--warning">{t('vision.degradedTag')}</span>}
                </Link>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </>
  )
}

// ---------------------------------------------------------------------------
// Session detail: analyze + review + summary
// ---------------------------------------------------------------------------
function SessionDetail({ sessionId }) {
  const { t } = useTranslation()
  const { user } = useAuth()
  const qc = useQueryClient()
  const [detections, setDetections] = useState(null)

  const session = useQuery({
    queryKey: queryKeys.vision.session(user?.id, sessionId),
    queryFn: ({ signal }) => visionApi.getSession(sessionId, signal),
    staleTime: STALE.short,
  })
  const candidates = useQuery({
    queryKey: queryKeys.vision.candidates(user?.id, sessionId),
    queryFn: ({ signal }) => visionApi.listCandidates(sessionId, {}, signal),
    staleTime: STALE.short,
  })
  const summary = useQuery({
    queryKey: queryKeys.vision.summary(user?.id, sessionId),
    queryFn: ({ signal }) => visionApi.getSummary(sessionId, signal),
    staleTime: STALE.short,
  })

  const refreshAll = () => {
    qc.invalidateQueries({ queryKey: queryKeys.vision.candidates(user?.id, sessionId) })
    qc.invalidateQueries({ queryKey: queryKeys.vision.summary(user?.id, sessionId) })
    qc.invalidateQueries({ queryKey: queryKeys.vision.session(user?.id, sessionId) })
  }

  const analyze = useMutation({
    mutationFn: async (file) => {
      const imageBase64 = await fileToBase64(file)
      return visionApi.analyzeFrame(sessionId, { imageBase64, wantEngagement: true })
    },
    onSuccess: (result) => {
      setDetections(result)
      refreshAll()
    },
  })

  const end = useMutation({
    mutationFn: () => visionApi.endSession(sessionId),
    onSuccess: refreshAll,
  })

  const degraded = session.data?.degraded || detections?.degraded

  return (
    <>
      <PageHeader
        title={session.data?.title || t('vision.sessionTitleFallback')}
        description={t('vision.detailDescription')}
        actions={<Link to="/app/vision" className="ui-btn ui-btn--secondary">{t('vision.back')}</Link>}
      />

      {session.isError && <ErrorState error={session.error} onRetry={session.refetch} />}
      {degraded && <DegradedBanner />}

      {session.data && (
        <Card title={t('vision.sessionInfo')}>
          <dl className="ui-kv">
            <div><dt>{t('vision.statusLabel')}</dt><dd data-testid="cv-status">{t(`vision.status_${(session.data.status || '').toLowerCase()}`, session.data.status)}</dd></div>
            <div><dt>{t('vision.framesLabel')}</dt><dd>{session.data.frameCount ?? 0}</dd></div>
            <div><dt>{t('vision.engineLabel')}</dt><dd>{session.data.engineKind || '—'}</dd></div>
          </dl>
          {session.data.status === 'Active' && (
            <Button variant="secondary" onClick={() => end.mutate()} loading={end.isPending}>
              {t('vision.endSession')}
            </Button>
          )}
        </Card>
      )}

      <Card title={t('vision.analyzeTitle')} description={t('vision.analyzeHelp')}>
        <input
          type="file"
          accept="image/*"
          className="ui-input"
          aria-label={t('vision.selectFrame')}
          data-testid="cv-frame-input"
          disabled={session.data?.status !== 'Active' || analyze.isPending}
          onChange={(e) => {
            const file = e.target.files?.[0]
            if (file) analyze.mutate(file)
          }}
        />
        {analyze.isPending && <p role="status">{t('vision.analyzing')}</p>}
        {analyze.isError && (
          <Alert variant="error" title={t('vision.aiUnavailableTitle')}>
            {t('vision.aiUnavailableBody')}
          </Alert>
        )}
        {detections && (
          <div data-testid="cv-detections">
            <p className="ui-muted">{t('vision.facesDetected', { count: detections.facesDetected ?? 0 })}</p>
            {(detections.results || []).length === 0 ? (
              <EmptyState title={t('vision.noDetections')} />
            ) : (
              <ul className="ui-list">
                {detections.results.map((f, i) => (
                  <li key={f.trackId || i} className="ui-list__item">
                    <span>{t('vision.track')}: <code>{f.trackId}</code></span>
                    <span>{t('vision.recognition')}: {t(`vision.rec_${f.recognitionStatus}`, f.recognitionStatus)} ({Math.round((f.recognitionConfidence ?? 0) * 100)}%)</span>
                    <span>{t('vision.emotion')}: {f.emotion}</span>
                    <span>{t('vision.engagement')}: {t(`vision.eng_${(f.engagement || '').toLowerCase()}`, f.engagement)}</span>
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}
      </Card>

      <Card title={t('vision.candidatesTitle')} description={t('vision.candidatesHelp')}>
        {candidates.isLoading && <p role="status">{t('states.loading', 'Loading…')}</p>}
        {candidates.isError && <ErrorState error={candidates.error} onRetry={candidates.refetch} />}
        {candidates.data && candidates.data.length === 0 && <EmptyState title={t('vision.noCandidates')} />}
        {candidates.data && candidates.data.length > 0 && (
          <ul className="ui-list" data-testid="cv-candidate-list">
            {candidates.data.map((c) => (
              <CandidateRow key={c.id} candidate={c} onReviewed={refreshAll} />
            ))}
          </ul>
        )}
      </Card>

      <Card title={t('vision.summaryTitle')}>
        {summary.isLoading && <p role="status">{t('states.loading', 'Loading…')}</p>}
        {summary.isError && <ErrorState error={summary.error} onRetry={summary.refetch} />}
        {summary.data && (
          <dl className="ui-kv" data-testid="cv-summary">
            <div><dt>{t('vision.engaged')}</dt><dd>{summary.data.engagedObservations ?? 0}</dd></div>
            <div><dt>{t('vision.disengaged')}</dt><dd>{summary.data.disengagedObservations ?? 0}</dd></div>
            <div><dt>{t('vision.notReady')}</dt><dd>{summary.data.notReadyObservations ?? 0}</dd></div>
            <div><dt>{t('vision.confirmedAttendance')}</dt><dd>{summary.data.confirmedAttendance ?? 0}</dd></div>
            <div><dt>{t('vision.pending')}</dt><dd>{summary.data.pendingCandidates ?? 0}</dd></div>
          </dl>
        )}
      </Card>
    </>
  )
}

function CandidateRow({ candidate, onReviewed }) {
  const { t } = useTranslation()
  const [studentId, setStudentId] = useState(candidate.mappedStudentId || '')
  const [status, setStatus] = useState('Present')
  const [notes, setNotes] = useState('')

  const reviewed = candidate.reviewStatus && candidate.reviewStatus !== 'Pending'

  const confirm = useMutation({
    mutationFn: () => visionApi.confirmCandidate(candidate.id, { studentId: studentId || undefined, status, notes: notes || undefined }),
    onSuccess: onReviewed,
  })
  const reject = useMutation({
    mutationFn: () => visionApi.rejectCandidate(candidate.id, { notes: notes || undefined }),
    onSuccess: onReviewed,
  })
  const override = useMutation({
    mutationFn: () => visionApi.overrideCandidate(candidate.id, { studentId, status, notes: notes || undefined }),
    onSuccess: onReviewed,
  })

  return (
    <li className="ui-list__item cv-candidate" data-testid="cv-candidate">
      <div className="cv-candidate__meta">
        <span><code>{candidate.trackId}</code></span>
        <span>{t('vision.recognition')}: {t(`vision.rec_${candidate.recognitionStatus}`, candidate.recognitionStatus)} ({Math.round((candidate.bestRecognitionConfidence ?? 0) * 100)}%)</span>
        <span className={`ui-badge ui-badge--${reviewed ? 'muted' : 'warning'}`} data-testid="cv-candidate-status">
          {t(`vision.review_${(candidate.reviewStatus || 'pending').toLowerCase()}`, candidate.reviewStatus)}
        </span>
        {candidate.mappedStudentName && <span>{t('vision.mapped')}: {candidate.mappedStudentName}</span>}
      </div>

      {!reviewed && (
        <div className="cv-candidate__actions">
          <input
            className="ui-input"
            placeholder={t('vision.studentId')}
            aria-label={t('vision.studentId')}
            value={studentId}
            onChange={(e) => setStudentId(e.target.value)}
          />
          <select className="ui-input" aria-label={t('vision.attendanceStatus')} value={status} onChange={(e) => setStatus(e.target.value)}>
            <option value="Present">{t('vision.present')}</option>
            <option value="Late">{t('vision.late')}</option>
            <option value="Absent">{t('vision.absent')}</option>
            <option value="Excused">{t('vision.excused')}</option>
          </select>
          <input
            className="ui-input"
            placeholder={t('vision.notes')}
            aria-label={t('vision.notes')}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />
          <Button onClick={() => confirm.mutate()} loading={confirm.isPending} disabled={!studentId}>
            {t('vision.confirm')}
          </Button>
          <Button variant="secondary" onClick={() => reject.mutate()} loading={reject.isPending}>
            {t('vision.reject')}
          </Button>
          <Button variant="secondary" onClick={() => override.mutate()} loading={override.isPending} disabled={!studentId}>
            {t('vision.override')}
          </Button>
          {(confirm.isError || reject.isError || override.isError) && (
            <ErrorState error={confirm.error || reject.error || override.error} />
          )}
        </div>
      )}
    </li>
  )
}
