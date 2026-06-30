import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { ArrowRight, Check, ClipboardList, Clock, FileText, PlayCircle } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { Button, Chip, Card } from '../../../shared/ui'
import { QueryBoundary, EmptyState, ErrorState } from '../../../shared/feedback'
import { useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { displayValue, formatDate, getField, itemId } from '../../../features/student/studentUtils'
import { getSubjectTheme } from '../../../features/student/theme'
import { queryKeys } from '../../../lib/query/keys'

const STATUS_LABELS = {
  available: 'Available',
  in_progress: 'In progress',
  submitted: 'Submitted',
  graded: 'Graded',
  closed: 'Closed',
}
function statusLabel(status) {
  return STATUS_LABELS[status] || status
}

function QuizzesPage({ userId, locale }) {
  const { t } = useTranslation()
  const { quizId } = useParams()
  const query = useStudentQuery(queryKeys.student.quizzes(userId), (signal) => studentApi.assignedQuizzes(signal))
  const [activeTab, setActiveTab] = useState('all')

  if (quizId) return <QuizDetails userId={userId} quizId={quizId} list={query} locale={locale} />

  const tabs = [
    { key: 'all', label: t('student.quizzes.tabs.all', 'All') },
    { key: 'available', label: t('student.quizzes.tabs.available', 'Available') },
    { key: 'in_progress', label: t('student.quizzes.tabs.inProgress', 'In progress') },
    { key: 'submitted', label: t('student.quizzes.tabs.submitted', 'Submitted') },
    { key: 'graded', label: t('student.quizzes.tabs.graded', 'Graded') },
  ]

  return (
    <>
      <div className="student-dashboard__welcome-header" style={{ marginBottom: '20px' }}>
        <h1 className="student-dashboard__welcome-title" style={{ fontSize: '28px', fontWeight: 800, margin: '0 0 6px' }}>
          {t('student.quizzes.title', 'Quizzes')}
        </h1>
        <p className="student-dashboard__welcome-subtitle" style={{ color: 'var(--text-dim)', margin: 0 }}>
          {t('student.quizzes.subtitle', 'Your assessments and results')}
        </p>
      </div>

      {/* Tabs */}
      <div style={{ display: 'flex', gap: '6px', marginBottom: '20px', borderBottom: '1px solid var(--border)', overflowX: 'auto' }}>
        {tabs.map((tab) => {
          const active = activeTab === tab.key
          return (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key)}
              style={{
                background: 'none',
                border: 'none',
                borderBottom: `2px solid ${active ? 'var(--brand)' : 'transparent'}`,
                color: active ? 'var(--brand)' : 'var(--text-dim)',
                fontWeight: active ? 700 : 500,
                padding: '10px 14px',
                cursor: 'pointer',
                fontSize: '14px',
                whiteSpace: 'nowrap',
                marginBottom: '-1px',
                transition: 'all 0.12s ease',
              }}
            >
              {tab.label}
            </button>
          )
        })}
      </div>

      <QueryBoundary query={query} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.quizzes')} emptyIcon={ClipboardList}>
        {(items) => {
          const filteredItems = items.filter((item) => {
            const status = String(getField(item, 'status') || 'available').toLowerCase()
            if (activeTab === 'all') return true
            return status === activeTab
          })

          if (filteredItems.length === 0) {
            return <EmptyState icon={ClipboardList} title={t('student.empty.quizzes', 'Nothing here')} message={t('student.quizzes.emptyCategory', 'You have no quizzes in this category.')} />
          }

          return (
            <div className="student-grid student-grid--3cols">
              {filteredItems.map((item) => {
                const theme = getSubjectTheme(item)
                const status = String(getField(item, 'status') || 'available').toLowerCase()
                const qCount = getField(item, 'questionCount')
                const duration = getField(item, 'timeLimitMinutes')
                const percentage = getField(item, 'percentage')
                const latestAttemptId = getField(item, 'latestAttemptId')
                const isGraded = status === 'graded'
                const isResultViewable = (isGraded || status === 'submitted') && latestAttemptId

                const quizItemId = itemId(item, ['quizId', 'QuizId', 'id', 'Id'])

                return (
                  <Card key={quizItemId} className="flex flex-col h-full">
                    <div className="flex justify-between items-start mb-3">
                      <div
                        style={{
                          width: '44px',
                          height: '44px',
                          borderRadius: '12px',
                          backgroundColor: `${theme.color}18`,
                          color: theme.color,
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                        }}
                      >
                        <ClipboardList size={22} />
                      </div>
                      {isGraded && percentage != null ? (
                        <Chip tone="success">{Math.round(percentage)}%</Chip>
                      ) : (
                        <Chip tone={status === 'available' ? 'brand' : 'neutral'}>
                          {t(`student.quizzes.status.${status}`, statusLabel(status))}
                        </Chip>
                      )}
                    </div>

                    <h3 className="[margin:0_0_6px] text-[17px] font-bold text-ink grow">
                      {displayValue(item)}
                    </h3>

                    <div className="flex gap-3.5 text-muted text-[13px] mb-4 flex-wrap">
                      {qCount != null && <span>{qCount} Q</span>}
                      {duration ? <span>{duration} min</span> : null}
                      {(getField(item, 'dueDate') || getField(item, 'dueAt')) && (
                        <span style={{ color: isGraded ? 'var(--success)' : 'var(--orange)', fontWeight: 600 }}>
                          {formatDate(getField(item, 'dueDate') || getField(item, 'dueAt'), locale)}
                        </span>
                      )}
                    </div>

                    {isResultViewable ? (
                      <Link
                        to={`/app/student/quiz-attempts/${latestAttemptId}/result`}
                        className="ui-btn ui-btn--secondary"
                        style={{ width: '100%', textDecoration: 'none' }}
                      >
                        {t('student.quizzes.viewResultBtn', 'View result')}
                      </Link>
                    ) : status === 'in_progress' && latestAttemptId ? (
                      <Link
                        to={`/app/student/quiz-attempts/${latestAttemptId}`}
                        className="ui-btn ui-btn--primary"
                        style={{ width: '100%', textDecoration: 'none' }}
                      >
                        {t('student.quizzes.continueBtn', 'Continue')}
                      </Link>
                    ) : (
                      <Link
                        to={`/app/student/quizzes/${quizItemId}`}
                        className="ui-btn ui-btn--primary"
                        aria-disabled={status === 'closed'}
                        style={{ width: '100%', textDecoration: 'none', ...(status === 'closed' ? { pointerEvents: 'none', opacity: 0.5 } : {}) }}
                      >
                        {status === 'closed' ? t('student.quizzes.closedBtn', 'Closed') : t('student.quizzes.startBtn', 'Start quiz')}
                      </Link>
                    )}
                  </Card>
                )
              })}
            </div>
          )
        }}
      </QueryBoundary>
    </>
  )
}

function QuizDetails({ userId, quizId, list, locale }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const isAr = locale === 'ar'

  const history = useStudentQuery(
    queryKeys.student.previousAttempts(userId, quizId),
    (signal) => studentApi.previousAttempts(quizId, signal)
  )

  const start = useMutation({
    mutationFn: () => studentApi.startQuiz(quizId),
    onSuccess: (attempt) => {
      qc.invalidateQueries({ queryKey: queryKeys.student.quizzes(userId) })
      navigate(`/app/student/quiz-attempts/${itemId(attempt)}`)
    }
  })

  const item = list.data?.find((q) => itemId(q, ['quizId', 'QuizId', 'id', 'Id']) === quizId)

  const title = displayValue(item) || t('student.quizzes.details')
  const desc = getField(item, 'description') || getField(item, 'desc') || ''
  const qCount = getField(item, 'questionCount')
  const duration = getField(item, 'timeLimitMinutes')
  const attemptsUsed = getField(item, 'attemptsUsed') ?? 0
  const maxAttempts = getField(item, 'maxAttempts')
  const subjectName = getField(item, 'subjectName')

  return (
    <>
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 mb-[22px] text-sm text-muted">
        <Link to="/app/student/quizzes" style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {t('student.quizzes.title', 'Quizzes')}
        </Link>
        <span className="text-faint">{isAr ? '‹' : '›'}</span>
        <span className="font-semibold text-ink">{title}</span>
      </div>

      {start.isError && <ErrorState error={start.error} />}

      <div className="max-w-[720px] [margin:0_auto]">
        <Card>
          <div className="flex items-center gap-3.5 mb-[18px]">
            <div style={{ width: '56px', height: '56px', borderRadius: '15px', background: 'var(--brand-soft)', display: 'flex', alignItems: 'center', justify: 'center', color: 'var(--brand)' }}>
              <ClipboardList size={28} />
            </div>
            <div>
              <h1 className="m-0 text-[26px] font-extrabold text-ink">
                {title}
              </h1>
              {subjectName && (
                <div className="text-muted text-sm mt-0.5">
                  {subjectName}
                </div>
              )}
            </div>
          </div>

          {desc && (
            <p className="text-muted leading-[1.7] text-[15px] mb-[18px]">
              {desc}
            </p>
          )}

          {/* Quiz metadata grid */}
          <div className="student-quiz-detail-stats">
            {/* Questions */}
            <div className="student-quiz-detail-stat">
              <ClipboardList size={20} className="student-quiz-detail-stat__icon" />
              <div className="student-quiz-detail-stat__value">{qCount ?? '—'}</div>
              <div className="student-quiz-detail-stat__label">{t('student.quizzes.stats.questions', 'Questions')}</div>
            </div>

            {/* Time */}
            <div className="student-quiz-detail-stat">
              <Clock size={20} className="student-quiz-detail-stat__icon" />
              <div className="student-quiz-detail-stat__value">{duration ? `${duration} min` : '—'}</div>
              <div className="student-quiz-detail-stat__label">{t('student.quizzes.stats.timeLimit', 'Time limit')}</div>
            </div>

            {/* Attempts */}
            <div className="student-quiz-detail-stat">
              <Check size={20} className="student-quiz-detail-stat__icon" />
              <div className="student-quiz-detail-stat__value">{maxAttempts ? `${attemptsUsed}/${maxAttempts}` : attemptsUsed}</div>
              <div className="student-quiz-detail-stat__label">{t('student.quizzes.stats.attempts', 'Attempts')}</div>
            </div>
          </div>

          {/* Yellow Warning Banner */}
          <div className="bg-[var(--warning-bg)] rounded-soft p-3.5 flex gap-2.5 mb-5">
            <ClipboardList size={20} className="text-warning shrink-0" />
            <div className="text-[13px] text-warning leading-[1.5]">
              {isAr
                ? 'بمجرد البدء يبدأ المؤقت. تأكد من اتصال مستقر. بالتوفيق!'
                : 'Once you start, the timer begins. Make sure you have a stable connection. Good luck!'}
            </div>
          </div>

          {/* Action buttons */}
          <div className="flex gap-2.5 flex-wrap">
            <Button
              onClick={() => start.mutate()}
              loading={start.isPending}
              size="lg"
            >
              <PlayCircle size={18} className="me-1.5" />
              {t('student.quizzes.start', 'Start quiz')}
            </Button>
            <Button
              variant="secondary"
              size="lg"
              onClick={() => navigate('/app/student/subjects')}
            >
              {t('student.quizzes.reviewLessonFirst', 'Review lesson first')}
            </Button>
          </div>
        </Card>

        {/* Previous attempts */}
        <div className="mt-4">
          <Card>
            <h3 className="[margin:0_0_12px] text-base font-bold text-ink">
              {t('student.quizzes.previousAttempts', 'Previous attempts')}
            </h3>

            <QueryBoundary query={history} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.attempts')} emptyIcon={ClipboardList}>
              {(items) => (
                <div className="flex flex-col gap-2.5">
                  {items.map((attempt) => (
                    <Link
                      key={itemId(attempt)}
                      to={`/app/student/quiz-attempts/${itemId(attempt)}/result`}
                      className="student-material-row no-underline"
                     
                    >
                      <div className="student-material-row__icon-tile">
                        <FileText size={20} />
                      </div>
                      <div className="student-material-row__content">
                        <div className="student-material-row__title">
                          {`${t('student.quizzes.attempt', 'Attempt')} ${getField(attempt, 'attemptNumber') ?? ''}`.trim()}
                        </div>
                        <div className="student-material-row__subtitle">
                          {getField(attempt, 'status') === 'Graded' && getField(attempt, 'percentage') != null
                            ? `${Math.round(getField(attempt, 'percentage'))}%`
                            : t('student.quizzes.status.submitted', 'Submitted')}
                        </div>
                      </div>
                      <div className="student-material-row__action">
                        <ArrowRight size={18} />
                      </div>
                    </Link>
                  ))}
                </div>
              )}
            </QueryBoundary>
          </Card>
        </div>
      </div>
    </>
  )
}

export default function StudentQuizzesPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <QuizzesPage userId={userId} locale={locale} {...props} />
}
