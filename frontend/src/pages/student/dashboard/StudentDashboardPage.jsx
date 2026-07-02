import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ArrowRight, Flame, PieChart, PlayCircle, Sparkles, Trophy } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { Avatar } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Loading } from '../../../features/student/Loading'
import { Ring } from '../../../shared/charts'
import { useAuth } from '../../../features/auth/AuthContext'
import { percentOf, useStudentQuery } from '../../../features/student/helpers'
import { studentApi } from '../../../features/student/studentApi'
import { toItems, toObject } from '../../../features/student/studentSchemas'
import { displayValue, getField, itemId, settledData } from '../../../features/student/studentUtils'
import { getSubjectTheme } from '../../../features/student/theme'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { isDemoEnabled } from '../../../demo/isDemoEnabled'
import { DEMO_SUBJECTS } from '../../../demo/studentDemoData'

function AssignedLessonCard({ subject }) {
  const { t } = useTranslation()
  const name = displayValue(subject)
  const id = itemId(subject)
  const progress = percentOf(subject) ?? 0

  // Presentational color/icon by subject category. Teacher/units come from real
  // backend fields; the sample values in `theme` are empty unless demo mode is on.
  const theme = getSubjectTheme(subject)
  const IconComponent = theme.icon
  const teacher = getField(subject, 'teacherName') || getField(subject, 'teacher') || theme.teacher
  const units = getField(subject, 'units') || theme.units || 0

  return (
    <Link to={`/app/student/subjects/${id}`} className="assigned-lesson-card">
      <div
        className="assigned-lesson-card__banner"
        style={{
          background: `linear-gradient(135deg, ${theme.color}, rgba(255, 255, 255, 0.25)), ${theme.color}`
        }}
      >
        <div className="assigned-lesson-card__banner-overlay" />
        <IconComponent size={44} className="assigned-lesson-card__banner-icon" />
      </div>
      <div className="assigned-lesson-card__body">
        <div className="assigned-lesson-card__header">
          <h3 className="assigned-lesson-card__title">{name}</h3>
          {units > 0 && (
            <span className="assigned-lesson-card__units-pill">
              {units} {t('student.units.title', 'Units')}
            </span>
          )}
        </div>
        {teacher && (
          <div className="assigned-lesson-card__teacher">
            <Avatar name={teacher} size={26} />
            <span className="assigned-lesson-card__teacher-name">{teacher}</span>
          </div>
        )}
        <div className="assigned-lesson-card__progress-info">
          <span className="assigned-lesson-card__progress-label">{t('student.progress.title', 'Progress')}</span>
          <span className="assigned-lesson-card__progress-pct" style={{ color: theme.color }}>
            {progress}%
          </span>
        </div>
        <div className="assigned-lesson-card__progress-bar-container">
          <div
            className="assigned-lesson-card__progress-bar"
            style={{
              width: `${progress}%`,
              backgroundColor: theme.color
            }}
          />
        </div>
      </div>
    </Link>
  )
}

function DashboardPage({ userId }) {
  const { t } = useTranslation()
  const { user } = useAuth()
  const studentName = user?.fullName || user?.userName || t('student.dashboard.student', 'Student')

  // Only queries whose data is actually rendered below — homework/notifications/
  // office-hours were previously fetched here too but never shown anywhere on
  // this page (dead over-fetching); each has its own dedicated page already.
  const subjects = useStudentQuery(queryKeys.student.subjects(userId), (signal) => studentApi.subjects(signal), { staleTime: STALE.medium })
  const quizzes = useStudentQuery(queryKeys.student.quizzes(userId), (signal) => studentApi.assignedQuizzes(signal))
  const progress = useStudentQuery(queryKeys.student.progress(userId), (signal) => studentApi.progress(userId, signal))
  const badges = useStudentQuery(queryKeys.student.badges(userId), (signal) => studentApi.badges(userId, signal), { staleTime: STALE.medium })
  const points = useStudentQuery(queryKeys.student.points(userId), (signal) => studentApi.points(userId, signal), { staleTime: STALE.short })

  const isInitialLoading = subjects.isLoading || quizzes.isLoading || progress.isLoading || badges.isLoading || points.isLoading
  const criticalError = subjects.error || quizzes.error || progress.error || badges.error || points.error
  const retryAll = () => { subjects.refetch(); quizzes.refetch(); progress.refetch(); badges.refetch(); points.refetch() }

  const progressRecommendations = progress.data ? toItems(settledData(progress.data.recommendations)) : []
  const earnedBadges = badges.data ? toItems(settledData(badges.data.earned)) : []
  const subjectItems = toItems(subjects.data)
  const quizItems = toItems(quizzes.data)

  // Real metrics only — when a source is empty/unavailable the value is null and the UI shows a
  // neutral placeholder rather than a fabricated number.
  const avgProgress = subjectItems.length > 0
    ? Math.round(subjectItems.reduce((acc, s) => acc + (percentOf(s) ?? 0), 0) / subjectItems.length)
    : null

  const streak = badges.data ? toObject(settledData(badges.data.streak)) : null
  const currentStreak = streak ? (getField(streak, 'current') ?? getField(streak, 'Current') ?? getField(streak, 'currentCount') ?? getField(streak, 'CurrentCount') ?? 0) : 0
  const daysThisWeek = streak ? (getField(streak, 'daysThisWeek') ?? getField(streak, 'DaysThisWeek') ?? 0) : 0

  const totalPoints = points.data ? (getField(points.data, 'totalPoints') ?? getField(points.data, 'TotalPoints') ?? 0) : 0

  // Featured assessment — only a REAL assigned quiz; otherwise the hero is hidden.
  const featuredQuiz = quizItems.length > 0 ? quizItems[0] : null
  const featuredId = featuredQuiz ? itemId(featuredQuiz, ['quizId', 'QuizId', 'id', 'Id']) : null
  const featuredName = featuredQuiz ? displayValue(featuredQuiz) : ''
  const featuredDesc = featuredQuiz ? (getField(featuredQuiz, 'description') || getField(featuredQuiz, 'desc') || '') : ''
  const featuredQCount = featuredQuiz ? getField(featuredQuiz, 'questionCount') : null
  const featuredDuration = featuredQuiz ? getField(featuredQuiz, 'timeLimitMinutes') : null
  const topRecommendation = progressRecommendations[0] || null

  return (
    <>
      <div className="student-dashboard__welcome-header">
        <h1 className="student-dashboard__welcome-title">
          {t('student.dashboard.welcomeBack', 'Welcome back, {{name}}', { name: studentName })}
        </h1>
        <p className="student-dashboard__welcome-subtitle">
          {t('student.dashboard.welcomeSubtitle', "Here's what's happening with your learning journey today")}
        </p>
      </div>

      {isInitialLoading && <Loading />}
      {!isInitialLoading && criticalError && <ErrorState error={criticalError} onRetry={retryAll} />}
      {!isInitialLoading && !criticalError && (
        <>
      <div className="student-dashboard__stats-grid">
        {/* Learning progress card */}
        <div className="stats-card stats-card--progress">
          <div className="stats-card__header">
            <PieChart size={18} className="stats-card__header-icon text-brand" />
            <span className="stats-card__header-title">{t('student.dashboard.learningProgress', 'Learning progress')}</span>
          </div>
          <div className="stats-card__content">
            <Ring value={avgProgress ?? 0} size={104} stroke={12} centerLabel={avgProgress == null ? '—' : `${avgProgress}%`} />
            <div className="stats-card__meta">
              <span className="stats-card__meta-label">{t('student.dashboard.overallCompletion', 'overall course completion')}</span>
            </div>
          </div>
        </div>

        {/* Weekly streak card */}
        <div className="stats-card stats-card--streak">
          <div className="stats-card__header">
            <Flame size={18} className="stats-card__header-icon text-orange" />
            <span className="stats-card__header-title">{t('student.dashboard.weeklyStreak', 'Weekly streak')}</span>
          </div>
          <div className="stats-card__content">
            <div className="stats-card__streak-numbers">
              <div className="stats-card__streak-number-item">
                <span className="stats-card__streak-value">{daysThisWeek}</span>
                <span className="stats-card__streak-label">{t('student.dashboard.daysThisWeek', 'Days this week')}</span>
              </div>
              <div className="stats-card__streak-number-item">
                <span className="stats-card__streak-value">{currentStreak}</span>
                <span className="stats-card__streak-label">{t('student.dashboard.dayStreak', 'Day streak')}</span>
              </div>
            </div>
            <div className="stats-card__streak-pills">
              {[1, 2, 3, 4, 5, 6, 7].map((d) => {
                const active = d <= daysThisWeek
                return (
                  <div
                    key={d}
                    className={`stats-card__streak-pill ${active ? 'is-active' : ''}`}
                    style={{
                      backgroundColor: active ? 'var(--orange)' : 'var(--border-2)',
                      color: active ? '#fff' : 'var(--text-dim)'
                    }}
                  >
                    {d}
                  </div>
                )
              })}
            </div>
          </div>
        </div>

        {/* Total points card */}
        <div className="stats-card stats-card--points">
          <div className="stats-card__header">
            <Trophy size={18} className="stats-card__header-icon text-purple" />
            <span className="stats-card__header-title">{t('student.dashboard.totalPoints', 'Total points')}</span>
          </div>
          <div className="stats-card__content">
            <span className="stats-card__points-value">{totalPoints.toLocaleString()}</span>
            {earnedBadges.length > 0 && (
              <span className="stats-card__points-rank">{t('student.dashboard.badgesEarned', '{{count}} badges earned', { count: earnedBadges.length })}</span>
            )}
          </div>
        </div>
      </div>

      {/* Featured assessment hero card — only when a real quiz is assigned. */}
      {featuredQuiz && (
        <div className="student-dashboard__hero-card">
          <div className="student-dashboard__hero-pattern" />
          <div className="student-dashboard__hero-content">
            <div className="student-dashboard__hero-pill-container">
              <span className="student-dashboard__hero-pill">
                {t('student.dashboard.featuredAssessment', 'Featured assessment')}
              </span>
            </div>
            <h2 className="student-dashboard__hero-title">{featuredName}</h2>
            {(featuredQCount != null || featuredDuration != null) && (
              <div className="student-dashboard__hero-meta">
                {featuredQCount != null && <>{featuredQCount} {t('student.quizzes.questionCount', 'Questions')}</>}
                {featuredQCount != null && featuredDuration != null && ' · '}
                {featuredDuration != null && <>{featuredDuration} {t('student.quizzes.duration', 'Minutes')}</>}
              </div>
            )}
            {featuredDesc && <p className="student-dashboard__hero-desc">{featuredDesc}</p>}
            <div className="student-dashboard__hero-actions">
              <Link to={`/app/student/quizzes/${featuredId}`} className="ui-btn student-dashboard__hero-btn-primary">
                <PlayCircle size={16} aria-hidden="true" />
                <span>{t('student.dashboard.startQuiz', 'Start quiz')}</span>
              </Link>
              <Link to="/app/student/subjects" className="ui-btn student-dashboard__hero-btn-secondary">
                {t('student.dashboard.reviewLesson', 'Review lessons')}
              </Link>
            </div>
          </div>
        </div>
      )}

      {/* Assigned lessons section */}
      <section className="student-dashboard__lessons-section">
        <div className="student-dashboard__section-header">
          <h2 className="student-dashboard__section-title">
            {t('student.dashboard.assignedLessons', 'Assigned lessons')}
          </h2>
          <Link to="/app/student/subjects" className="ui-btn ui-btn--soft">
            {t('student.viewAll', 'View all')}
          </Link>
        </div>
        <div className="student-dashboard__lessons-grid">
          {subjectItems.slice(0, 3).map((subject) => (
            <AssignedLessonCard key={itemId(subject)} subject={subject} />
          ))}
          {/* Demo-only sample cards when the API returns nothing; never in production. */}
          {subjectItems.length === 0 &&
            isDemoEnabled() &&
            DEMO_SUBJECTS.slice(0, 3).map((s) => <AssignedLessonCard key={s.id} subject={s} />)}
        </div>
      </section>

      {/* AI recommendation banner — only when a real stored recommendation exists. */}
      {topRecommendation && (
        <div className="student-dashboard__ai-banner">
          <div className="student-dashboard__ai-banner-icon-container">
            <Sparkles size={28} className="student-dashboard__ai-banner-icon" />
          </div>
          <div className="student-dashboard__ai-banner-text-container">
            <div className="student-dashboard__ai-banner-header">
              <h3 className="student-dashboard__ai-banner-title">
                {displayValue(topRecommendation, ['title', 'Title']) || t('student.dashboard.aiRecommended', 'AI recommended for you')}
              </h3>
              <span className="student-dashboard__ai-banner-pill">AI</span>
            </div>
            <p className="student-dashboard__ai-banner-desc">
              {getField(topRecommendation, 'body') || getField(topRecommendation, 'Body') || ''}
            </p>
          </div>
          <div className="student-dashboard__ai-banner-action">
            <Link to="/app/student/recommendations" className="ui-btn ui-btn--primary student-dashboard__ai-banner-btn">
              <span>{t('student.dashboard.startNow', 'Start now')}</span>
              <ArrowRight size={16} aria-hidden="true" />
            </Link>
          </div>
        </div>
      )}
        </>
      )}
    </>
  )
}

// =============================================================================
// Subjects / units / lessons / materials
// =============================================================================
// getSubjectTheme moved to ../../features/student/theme.js — presentational
// color+icon there, with sample teacher/progress surfaced only in demo mode.

export default function StudentDashboardPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <DashboardPage userId={userId} locale={locale} {...props} />
}
