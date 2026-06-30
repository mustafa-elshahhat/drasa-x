import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { ArrowRight, Check, ClipboardList, Clock, FileText, PlayCircle } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { Button } from '../../../components/ui/Button'
import { Chip } from '../../../components/ui/Chip'
import { Card } from '../../../components/ui/PageHeader'
import { QueryBoundary } from '../../../components/ui/QueryBoundary'
import { EmptyState, ErrorState } from '../../../components/ui/states'
import { useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { displayValue, formatDate, getField, itemId } from '../../../features/student/studentUtils'
import { getSubjectTheme } from '../../../features/student/theme'
import { queryKeys } from '../../../lib/query/keys'

function QuizzesPage({ userId, locale }) {
  const { t } = useTranslation()
  const { quizId } = useParams()
  const query = useStudentQuery(queryKeys.student.quizzes(userId), (signal) => studentApi.assignedQuizzes(signal))
  const [activeTab, setActiveTab] = useState('all')

  if (quizId) return <QuizDetails userId={userId} quizId={quizId} list={query} locale={locale} />

  const tabs = [
    { key: 'all', label: t('student.quizzes.tabs.all', 'All') },
    { key: 'available', label: t('student.quizzes.tabs.available', 'Available') },
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
            if (activeTab === 'available') return status === 'available'
            if (activeTab === 'graded') return status === 'graded' || status === 'submitted'
            return true
          })

          if (filteredItems.length === 0) {
            return <EmptyState icon={ClipboardList} title={t('student.empty.quizzes', 'Nothing here')} message={t('student.quizzes.emptyCategory', 'You have no quizzes in this category.')} />
          }

          return (
            <div className="student-grid student-grid--3cols">
              {filteredItems.map((item) => {
                const theme = getSubjectTheme(item)
                const status = String(getField(item, 'status') || 'available').toLowerCase()
                const qCount = getField(item, 'questionsCount') || getField(item, 'q') || 20
                const duration = getField(item, 'duration') || getField(item, 'min') || 30
                const score = getField(item, 'score') || getField(item, 'scorePercentage')
                const isGraded = status === 'graded'

                const quizItemId = itemId(item, ['quizId', 'QuizId', 'id', 'Id'])

                return (
                  <Card key={quizItemId} style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '12px' }}>
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
                      {isGraded ? (
                        <Chip tone="success">{score}%</Chip>
                      ) : (
                        <Chip tone="brand">{t('student.quizzes.available', 'Available')}</Chip>
                      )}
                    </div>

                    <h3 style={{ margin: '0 0 6px', fontSize: '17px', fontWeight: 700, color: 'var(--text)', flexGrow: 1 }}>
                      {displayValue(item)}
                    </h3>

                    <div style={{ display: 'flex', gap: '14px', color: 'var(--text-dim)', fontSize: '13px', marginBottom: '16px', flexWrap: 'wrap' }}>
                      <span>{qCount} Q</span>
                      <span>{duration} min</span>
                      <span style={{ color: isGraded ? 'var(--success)' : 'var(--orange)', fontWeight: 600 }}>
                        {formatDate(getField(item, 'dueDate') || getField(item, 'dueAt'), locale)}
                      </span>
                    </div>

                    {isGraded ? (
                      <Link
                        to={`/app/student/quiz-attempts/${quizItemId}/result`}
                        className="ui-btn ui-btn--secondary"
                        style={{ width: '100%', textDecoration: 'none' }}
                      >
                        {t('student.quizzes.viewResultBtn', 'View result')}
                      </Link>
                    ) : (
                      <Link
                        to={`/app/student/quizzes/${quizItemId}`}
                        className="ui-btn ui-btn--primary"
                        style={{ width: '100%', textDecoration: 'none' }}
                      >
                        {t('student.quizzes.startBtn', 'Start quiz')}
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
  const theme = getSubjectTheme(item)

  const title = displayValue(item) || t('student.quizzes.details')
  const desc = getField(item, 'description') || getField(item, 'desc') || (isAr ? 'يركّز هذا الاختبار على فهمك للمنهج وتطبيقاته.' : 'Tests your understanding of the curriculum details.')
  const qCount = getField(item, 'questionsCount') || getField(item, 'q') || 20
  const duration = getField(item, 'duration') || getField(item, 'min') || 30

  return (
    <>
      {/* Breadcrumb */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '22px', fontSize: '14px', color: 'var(--text-dim)' }}>
        <Link to="/app/student/quizzes" style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {t('student.quizzes.title', 'Quizzes')}
        </Link>
        <span style={{ color: 'var(--faint)' }}>{isAr ? '‹' : '›'}</span>
        <span style={{ fontWeight: 600, color: 'var(--text)' }}>{title}</span>
      </div>

      {start.isError && <ErrorState error={start.error} />}

      <div style={{ maxWidth: '720px', margin: '0 auto' }}>
        <Card>
          <div style={{ display: 'flex', alignItems: 'center', gap: '14px', marginBottom: '18px' }}>
            <div style={{ width: '56px', height: '56px', borderRadius: '15px', background: 'var(--brand-soft)', display: 'flex', alignItems: 'center', justify: 'center', color: 'var(--brand)' }}>
              <ClipboardList size={28} />
            </div>
            <div>
              <h1 style={{ margin: 0, fontSize: '26px', fontWeight: 800, color: 'var(--text)' }}>
                {title}
              </h1>
              <div style={{ color: 'var(--text-dim)', fontSize: '14px', marginTop: '2px' }}>
                {theme.teacher} &middot; {theme.units} {t('student.units.title', 'Units')}
              </div>
            </div>
          </div>

          <p style={{ color: 'var(--text-dim)', lineHeight: 1.7, fontSize: '15px', marginBottom: '18px' }}>
            {desc}
          </p>

          {/* Quiz metadata grid */}
          <div className="student-quiz-detail-stats">
            {/* Questions */}
            <div className="student-quiz-detail-stat">
              <ClipboardList size={20} className="student-quiz-detail-stat__icon" />
              <div className="student-quiz-detail-stat__value">{qCount}</div>
              <div className="student-quiz-detail-stat__label">{t('student.quizzes.stats.questions', 'Questions')}</div>
            </div>

            {/* Time */}
            <div className="student-quiz-detail-stat">
              <Clock size={20} className="student-quiz-detail-stat__icon" />
              <div className="student-quiz-detail-stat__value">{duration} min</div>
              <div className="student-quiz-detail-stat__label">{t('student.quizzes.stats.timeLimit', 'Time limit')}</div>
            </div>

            {/* Attempts */}
            <div className="student-quiz-detail-stat">
              <Check size={20} className="student-quiz-detail-stat__icon" />
              <div className="student-quiz-detail-stat__value">1</div>
              <div className="student-quiz-detail-stat__label">{t('student.quizzes.stats.attempts', 'Attempts')}</div>
            </div>
          </div>

          {/* Yellow Warning Banner */}
          <div style={{ background: 'var(--warning-bg)', borderRadius: 'var(--radius-sm)', padding: '14px', display: 'flex', gap: '10px', marginBottom: '20px' }}>
            <ClipboardList size={20} style={{ color: 'var(--warning)', flexShrink: 0 }} />
            <div style={{ fontSize: '13px', color: 'var(--warning)', lineHeight: 1.5 }}>
              {isAr
                ? 'بمجرد البدء يبدأ المؤقت. تأكد من اتصال مستقر. بالتوفيق!'
                : 'Once you start, the timer begins. Make sure you have a stable connection. Good luck!'}
            </div>
          </div>

          {/* Action buttons */}
          <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
            <Button
              onClick={() => start.mutate()}
              loading={start.isPending}
              size="lg"
            >
              <PlayCircle size={18} style={{ marginInlineEnd: '6px' }} />
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
        <div style={{ marginTop: '16px' }}>
          <Card>
            <h3 style={{ margin: '0 0 12px', fontSize: '16px', fontWeight: 700, color: 'var(--text)' }}>
              {t('student.quizzes.previousAttempts', 'Previous attempts')}
            </h3>

            <QueryBoundary query={history} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.attempts')} emptyIcon={ClipboardList}>
              {(items) => (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                  {items.map((attempt) => (
                    <Link
                      key={itemId(attempt)}
                      to={`/app/student/quiz-attempts/${itemId(attempt)}/result`}
                      className="student-material-row"
                      style={{ textDecoration: 'none' }}
                    >
                      <div className="student-material-row__icon-tile">
                        <FileText size={20} />
                      </div>
                      <div className="student-material-row__content">
                        <div className="student-material-row__title">
                          {displayValue(attempt) || `${t('student.quizzes.attempt')} ${itemId(attempt)}`}
                        </div>
                        <div className="student-material-row__subtitle">
                          {getField(attempt, 'score') ?? getField(attempt, 'scorePercentage')}%
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
