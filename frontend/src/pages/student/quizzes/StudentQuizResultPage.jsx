import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Check, Sparkles, Trophy } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { Chip, Card } from '../../../shared/ui'
import { NotEnoughData, QueryBoundary, ErrorState } from '../../../shared/feedback'
import { Ring } from '../../../shared/charts'
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
      <div className="flex items-center gap-2 mb-[22px] text-sm text-muted">
        <Link to="/app/student/quizzes" style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {t('student.quizzes.title', 'Quizzes')}
        </Link>
        <span className="text-faint">{isAr ? '‹' : '›'}</span>
        <span className="font-semibold text-ink">{t('student.quizzes.result', 'Result')}</span>
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
            <div className="max-w-[760px]">
              {/* Result Hero Card */}
              <Card>
                <div className="flex gap-6 items-center flex-wrap">
                  <Ring
                    value={pct}
                    size={140}
                    centerLabel={`${pct}%`}
                    stroke={12}
                    color={pct >= 85 ? 'var(--success)' : 'var(--warning)'}
                  />
                  <div className="flex-[1_1_240px]">
                    <div className="inline-flex items-center gap-2 bg-[var(--success-bg)] text-success [padding:6px_12px] rounded-[20px] font-bold text-[13px] mb-2.5">
                      <Check size={15} />
                      <span>{t('student.quizzes.passed', 'Passed')}</span>
                    </div>
                    <h1 className="[margin:0_0_6px] text-[28px] font-extrabold text-ink">
                      {t('student.quizzes.congrats', 'Great work!')}
                    </h1>
                    <p className="text-muted m-0 text-[15px]">
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
                <Card className="p-[18px]">
                  <div className="flex items-center gap-2 text-success mb-2">
                    <Check size={18} />
                    <span className="text-[13px] text-muted font-semibold">
                      {t('student.quizzes.correct', 'Correct')}
                    </span>
                  </div>
                  <div className="text-[26px] font-extrabold text-ink">{correct}</div>
                </Card>

                {/* Incorrect */}
                <Card className="p-[18px]">
                  <div className="flex items-center gap-2 text-danger mb-2">
                    <span style={{ color: 'var(--danger)', fontWeight: 700, fontSize: '18px', lineBreak: 'none', display: 'inline-flex', width: '18px', height: '18px', alignItems: 'center', justifyContent: 'center' }}>×</span>
                    <span className="text-[13px] text-muted font-semibold">
                      {t('student.quizzes.incorrect', 'Incorrect')}
                    </span>
                  </div>
                  <div className="text-[26px] font-extrabold text-ink">{total - correct}</div>
                </Card>

                {/* Points */}
                <Card className="p-[18px]">
                  <div className="flex items-center gap-2 text-purple mb-2">
                    <Trophy size={18} />
                    <span className="text-[13px] text-muted font-semibold">
                      {t('student.quizzes.pointsEarned', 'Points earned')}
                    </span>
                  </div>
                  <div className="text-[26px] font-extrabold text-ink">{pts}</div>
                </Card>
              </div>

              {/* Questions Review */}
              <Card>
                <h3 className="[margin:0_0_14px] font-bold text-ink text-base">
                  {t('student.quizzes.questionReview', 'Question review')}
                </h3>

                <div className="flex flex-col">
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
                              <Check size={16} className="text-success" />
                            ) : (
                              <span className="text-danger font-bold text-sm">×</span>
                            )}
                          </div>
                          <span className="flex-1 text-sm text-ink-2">{qText}</span>
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
                            <Check size={16} className="text-success" />
                          ) : (
                            <span className="text-danger font-bold text-sm">×</span>
                          )}
                        </div>
                        <span className="flex-1 text-sm text-ink-2">{item.q}</span>
                        <Chip tone={item.correct ? 'success' : 'danger'}>
                          {item.correct ? t('student.quizzes.correctState', 'Correct') : t('student.quizzes.reviewState', 'Review')}
                        </Chip>
                      </div>
                    ))
                  ) : (
                    <NotEnoughData />
                  )}
                </div>

                <div className="flex gap-2.5 mt-[18px] flex-wrap">
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
