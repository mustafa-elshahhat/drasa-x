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
          const status = String(getField(data, 'status') || '').toLowerCase()
          const isGraded = status === 'graded'
          const answers = toItems(getField(data, 'answers') || [])
          const title = getField(data, 'quizTitle') || ''

          // Awaiting manual grading: show an honest pending state, never a fabricated score.
          if (!isGraded) {
            return (
              <div className="max-w-[760px]">
                <Card>
                  <h1 className="[margin:0_0_8px] text-[22px] font-extrabold text-ink">{title || t('student.quizzes.result', 'Result')}</h1>
                  <NotEnoughData title={t('student.quizzes.resultPending', 'Your quiz was submitted and is awaiting grading.')} />
                  <div className="mt-4">
                    <Link to="/app/student/quizzes" className="ui-btn ui-btn--secondary" style={{ textDecoration: 'none' }}>
                      {t('student.quizzes.backToQuizzes', 'Back to quizzes')}
                    </Link>
                  </div>
                </Card>
              </div>
            )
          }

          const pct = Math.round(percentOf(data) ?? getField(data, 'percentage') ?? 0)
          const achieved = getField(data, 'achievedScore')
          const totalScore = getField(data, 'totalScore')
          // Real correctness counts come only from graded objective answers.
          const correct = answers.filter((a) => getField(a, 'isCorrect') === true).length
          const total = getField(data, 'questionCount') ?? answers.length
          const pts = achieved != null && totalScore != null ? `${achieved}/${totalScore}` : (achieved ?? '—')
          const passed = pct >= 50

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
                    color={pct >= 85 ? 'var(--success)' : pct >= 50 ? 'var(--warning)' : 'var(--danger)'}
                  />
                  <div className="flex-[1_1_240px]">
                    <div
                      className="inline-flex items-center gap-2 [padding:6px_12px] rounded-[20px] font-bold text-[13px] mb-2.5"
                      style={{ background: passed ? 'var(--success-bg)' : 'var(--danger-bg)', color: passed ? 'var(--success)' : 'var(--danger)' }}
                    >
                      <Check size={15} />
                      <span>{passed ? t('student.quizzes.passed', 'Passed') : t('student.quizzes.notPassed', 'Keep practicing')}</span>
                    </div>
                    <h1 className="[margin:0_0_6px] text-[28px] font-extrabold text-ink">
                      {title || t('student.quizzes.result', 'Result')}
                    </h1>
                    <p className="text-muted m-0 text-[15px]">
                      {isAr
                        ? `لقد حصلت على ${correct} من ${total}.`
                        : `You scored ${correct} out of ${total}.`}
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
                      // Correctness comes ONLY from the backend; never invented. Unknown → neutral.
                      const isCorrect = getField(ans, 'isCorrect')
                      const qText = getField(ans, 'questionText') || `${t('student.quizzes.questionIndex', 'Question')} ${i + 1}`
                      const correctAnswer = getField(ans, 'correctAnswer')
                      const explanation = getField(ans, 'explanation')
                      const tone = isCorrect === true ? 'success' : isCorrect === false ? 'danger' : 'neutral'
                      return (
                        <div
                          key={getField(ans, 'questionId') || i}
                          style={{
                            display: 'flex',
                            gap: '12px',
                            alignItems: 'flex-start',
                            padding: '12px 0',
                            borderBottom: i < answers.length - 1 ? '1px solid var(--border-2)' : 'none',
                          }}
                        >
                          <div
                            style={{
                              width: '30px',
                              height: '30px',
                              borderRadius: '50%',
                              background: isCorrect === true ? 'var(--success-bg)' : isCorrect === false ? 'var(--danger-bg)' : 'var(--border-2)',
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'center',
                              flexShrink: 0,
                            }}
                          >
                            {isCorrect === true ? (
                              <Check size={16} className="text-success" />
                            ) : isCorrect === false ? (
                              <span className="text-danger font-bold text-sm">×</span>
                            ) : (
                              <span className="text-muted font-bold text-sm">?</span>
                            )}
                          </div>
                          <div className="flex-1">
                            <span className="text-sm text-ink-2">{qText}</span>
                            {isCorrect === false && correctAnswer && (
                              <div className="text-[13px] text-success mt-1">
                                {t('student.quizzes.correctAnswer', 'Correct answer')}: {correctAnswer}
                              </div>
                            )}
                            {explanation && <div className="text-[13px] text-muted mt-1">{explanation}</div>}
                          </div>
                          <Chip tone={tone}>
                            {isCorrect === true
                              ? t('student.quizzes.correctState', 'Correct')
                              : isCorrect === false
                              ? t('student.quizzes.reviewState', 'Review')
                              : t('student.quizzes.pendingState', 'Pending')}
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
