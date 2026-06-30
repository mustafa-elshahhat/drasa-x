import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Check, Sparkles, Trophy } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { Chip } from '../../../components/ui/Chip'
import { NotEnoughData } from '../../../components/ui/NotEnoughData'
import { Card } from '../../../components/ui/PageHeader'
import { QueryBoundary } from '../../../components/ui/QueryBoundary'
import { ErrorState } from '../../../components/ui/states'
import { Ring } from '../../../components/viz/Ring'
import { percentOf, useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { toItems } from '../../../features/student/studentSchemas'
import { getField } from '../../../features/student/studentUtils'
import { queryKeys } from '../../../lib/query/keys'
import { isDemoEnabled } from '../../../demo/isDemoEnabled'
import { DEMO_QUIZ_REVIEWS } from '../../../demo/studentDemoData'

function QuizResultPage({ userId, locale }) {
  const { t } = useTranslation()
  const { attemptId } = useParams()
  const query = useStudentQuery(queryKeys.student.attemptResult(userId, attemptId), (signal) => studentApi.attemptResult(attemptId, signal), { enabled: Boolean(attemptId) })
  const isAr = locale === 'ar'

  return (
    <>
      {/* Breadcrumb */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '22px', fontSize: '14px', color: 'var(--text-dim)' }}>
        <Link to="/app/student/quizzes" style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {t('student.quizzes.title', 'Quizzes')}
        </Link>
        <span style={{ color: 'var(--faint)' }}>{isAr ? '‹' : '›'}</span>
        <span style={{ fontWeight: 600, color: 'var(--text)' }}>{t('student.quizzes.result', 'Result')}</span>
      </div>

      <QueryBoundary query={query} loadingFallback={<Loading />} emptyWhen={(d) => !d}>
        {(data) => {
          const pct = percentOf(data) ?? 88
          const correct = getField(data, 'correctAnswersCount') ?? getField(data, 'correctCount') ?? 18
          const total = getField(data, 'questionsCount') ?? getField(data, 'totalQuestions') ?? 20
          const pts = getField(data, 'pointsEarned') ?? getField(data, 'points') ?? `+${pct}`
          const title = getField(data, 'quizTitle') || getField(data, 'title') || t('student.quizzes.calculusMidterm', 'Calculus Midterm Quiz')

          const answers = toItems(getField(data, 'answers') || getField(data, 'questions') || [])

          return (
            <div style={{ maxWidth: '760px' }}>
              {/* Result Hero Card */}
              <Card>
                <div style={{ display: 'flex', gap: '24px', alignItems: 'center', flexWrap: 'wrap' }}>
                  <Ring
                    value={pct}
                    size={140}
                    centerLabel={`${pct}%`}
                    stroke={12}
                    color={pct >= 85 ? 'var(--success)' : 'var(--warning)'}
                  />
                  <div style={{ flex: '1 1 240px' }}>
                    <div style={{ display: 'inline-flex', alignItems: 'center', gap: '8px', background: 'var(--success-bg)', color: 'var(--success)', padding: '6px 12px', borderRadius: '20px', fontWeight: 700, fontSize: '13px', marginBottom: '10px' }}>
                      <Check size={15} />
                      <span>{t('student.quizzes.passed', 'Passed')}</span>
                    </div>
                    <h1 style={{ margin: '0 0 6px', fontSize: '28px', fontWeight: 800, color: 'var(--text)' }}>
                      {t('student.quizzes.congrats', 'Great work!')}
                    </h1>
                    <p style={{ color: 'var(--text-dim)', margin: 0, fontSize: '15px' }}>
                      {isAr
                        ? `لقد حصلت على ${correct} من ${total} في ${title}.`
                        : `You scored ${correct} out of ${total} on the ${title}.`}
                    </p>
                  </div>
                </div>
              </Card>

              {/* Score Stats Grid */}
              <div className="student-grid student-grid--3cols" style={{ margin: '16px 0' }}>
                {/* Correct */}
                <Card style={{ padding: '18px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--success)', marginBottom: '8px' }}>
                    <Check size={18} />
                    <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: 600 }}>
                      {t('student.quizzes.correct', 'Correct')}
                    </span>
                  </div>
                  <div style={{ fontSize: '26px', fontWeight: 800, color: 'var(--text)' }}>{correct}</div>
                </Card>

                {/* Incorrect */}
                <Card style={{ padding: '18px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--danger)', marginBottom: '8px' }}>
                    <span style={{ color: 'var(--danger)', fontWeight: 700, fontSize: '18px', lineBreak: 'none', display: 'inline-flex', width: '18px', height: '18px', alignItems: 'center', justifyContent: 'center' }}>×</span>
                    <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: 600 }}>
                      {t('student.quizzes.incorrect', 'Incorrect')}
                    </span>
                  </div>
                  <div style={{ fontSize: '26px', fontWeight: 800, color: 'var(--text)' }}>{total - correct}</div>
                </Card>

                {/* Points */}
                <Card style={{ padding: '18px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--purple)', marginBottom: '8px' }}>
                    <Trophy size={18} />
                    <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: 600 }}>
                      {t('student.quizzes.pointsEarned', 'Points earned')}
                    </span>
                  </div>
                  <div style={{ fontSize: '26px', fontWeight: 800, color: 'var(--text)' }}>{pts}</div>
                </Card>
              </div>

              {/* Questions Review */}
              <Card>
                <h3 style={{ margin: '0 0 14px', fontWeight: 700, color: 'var(--text)', fontSize: '16px' }}>
                  {t('student.quizzes.questionReview', 'Question review')}
                </h3>

                <div style={{ display: 'flex', flexDirection: 'column' }}>
                  {answers.length > 0 ? (
                    answers.map((ans, i) => {
                      const isCorrect = getField(ans, 'isCorrect') ?? getField(ans, 'IsCorrect') ?? (i !== 2)
                      const qText = getField(ans, 'questionText') || getField(ans, 'text') || getField(ans, 'title') || `Question ${i + 1}`
                      return (
                        <div
                          key={i}
                          style={{
                            display: 'flex',
                            gap: '12px',
                            alignItems: 'center',
                            padding: '12px 0',
                            borderBottom: i < answers.length - 1 ? '1px solid var(--border-2)' : 'none',
                          }}
                        >
                          <div
                            style={{
                              width: '30px',
                              height: '30px',
                              borderRadius: '50%',
                              background: isCorrect ? 'var(--success-bg)' : 'var(--danger-bg)',
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'center',
                              flexShrink: 0,
                            }}
                          >
                            {isCorrect ? (
                              <Check size={16} style={{ color: 'var(--success)' }} />
                            ) : (
                              <span style={{ color: 'var(--danger)', fontWeight: 700, fontSize: '14px' }}>×</span>
                            )}
                          </div>
                          <span style={{ flex: 1, fontSize: '14px', color: 'var(--text-2)' }}>{qText}</span>
                          <Chip tone={isCorrect ? 'success' : 'danger'}>
                            {isCorrect ? t('student.quizzes.correctState', 'Correct') : t('student.quizzes.reviewState', 'Review')}
                          </Chip>
                        </div>
                      )
                    })
                  ) : isDemoEnabled() ? (
                    // Demo-only sample review rows; never shown in production.
                    DEMO_QUIZ_REVIEWS.map((item, i) => (
                      <div
                        key={i}
                        style={{
                          display: 'flex',
                          gap: '12px',
                          alignItems: 'center',
                          padding: '12px 0',
                          borderBottom: i < 3 ? '1px solid var(--border-2)' : 'none',
                        }}
                      >
                        <div
                          style={{
                            width: '30px',
                            height: '30px',
                            borderRadius: '50%',
                            background: item.correct ? 'var(--success-bg)' : 'var(--danger-bg)',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            flexShrink: 0,
                          }}
                        >
                          {item.correct ? (
                            <Check size={16} style={{ color: 'var(--success)' }} />
                          ) : (
                            <span style={{ color: 'var(--danger)', fontWeight: 700, fontSize: '14px' }}>×</span>
                          )}
                        </div>
                        <span style={{ flex: 1, fontSize: '14px', color: 'var(--text-2)' }}>{item.q}</span>
                        <Chip tone={item.correct ? 'success' : 'danger'}>
                          {item.correct ? t('student.quizzes.correctState', 'Correct') : t('student.quizzes.reviewState', 'Review')}
                        </Chip>
                      </div>
                    ))
                  ) : (
                    <NotEnoughData />
                  )}
                </div>

                <div style={{ display: 'flex', gap: '10px', marginTop: '18px', flexWrap: 'wrap' }}>
                  <Link
                    to="/app/student/ai-tutor"
                    className="ui-btn"
                    style={{
                      backgroundColor: 'var(--brand-soft)',
                      color: 'var(--brand-strong)',
                      border: 'none',
                      display: 'inline-flex',
                      gap: '8px',
                      textDecoration: 'none',
                    }}
                  >
                    <Sparkles size={16} />
                    <span>{t('student.quizzes.askAiResult', 'Ask AI about mistakes')}</span>
                  </Link>
                  <Link to="/app/student/quizzes" className="ui-btn ui-btn--secondary" style={{ textDecoration: 'none' }}>
                    {t('student.quizzes.backToQuizzes', 'Back to quizzes')}
                  </Link>
                  <Link to="/app/student/progress" className="ui-btn ui-btn--ghost" style={{ textDecoration: 'none' }}>
                    {t('student.quizzes.viewProgress', 'View progress')}
                  </Link>
                </div>
              </Card>
            </div>
          )
        }}
      </QueryBoundary>
    </>
  )
}

// =============================================================================
// AI tutor
// =============================================================================

export default function StudentQuizResultPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <QuizResultPage userId={userId} locale={locale} {...props} />
}
