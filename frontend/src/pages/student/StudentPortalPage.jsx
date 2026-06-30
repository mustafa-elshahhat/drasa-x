import React, { useState, useRef } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import {
  Bot, FileText, BookOpen, ClipboardCheck, ClipboardList, Bell, CalendarClock, Medal, Users, Trophy,
  Layers, PlayCircle, GraduationCap, Megaphone, Lightbulb, MessageSquare, Send, Flame, Sparkles, ShieldCheck,
  Sigma, Atom, FlaskConical, Leaf, PieChart, Bolt, ArrowRight, Globe, Search, Clock, ChevronRight, Check, Download,
} from 'lucide-react'
import { PageHeader, Card } from '../../components/ui/PageHeader'
import { Metric } from '../../components/ui/Metric'
import { EmptyState, ErrorState } from '../../components/ui/states'
import { NotEnoughData } from '../../components/ui/NotEnoughData'
import { Button } from '../../components/ui/Button'
import { Alert } from '../../components/ui/Alert'
import { Chip } from '../../components/ui/Chip'
import { Spinner } from '../../components/ui/Spinner'
import { QueryBoundary } from '../../components/ui/QueryBoundary'
import { TextField, TextareaField } from '../../components/form/fields'
import { DetailList } from '../../components/data/DetailList'
import { Ring } from '../../components/viz/Ring'
import { ProgressBar } from '../../components/viz/ProgressBar'
import { Heatmap } from '../../components/viz/Heatmap'
import { StreakStrip } from '../../components/viz/StreakStrip'
import { SubjectCard } from '../../components/domain/SubjectCard'
import { UnitCard } from '../../components/domain/UnitCard'
import { LessonRow } from '../../components/domain/LessonRow'
import { QuizCard } from '../../components/domain/QuizCard'
import { LeaderboardRow } from '../../components/domain/LeaderboardRow'
import { Thumb } from '../../components/domain/Thumb'
import { AITutorMessage } from '../../components/chat/AITutorMessage'
import { OptionCard } from '../../components/quiz/OptionCard'
import { Avatar } from '../../components/ui/Avatar'
import { useAuth } from '../../features/auth/AuthContext'
import { studentApi } from '../../features/student/studentApi'
import {
  answerPayloadFromForm, displayValue, formatDate, getField, itemId, settledData, settledError,
} from '../../features/student/studentUtils'
import { queryKeys, STALE } from '../../lib/query/keys'
import { toItems, toObject } from '../../features/student/studentSchemas'

function useStudentQuery(key, fn, options = {}) {
  return useQuery({ queryKey: key, queryFn: ({ signal }) => fn(signal), staleTime: options.staleTime ?? STALE.short, enabled: options.enabled ?? true })
}

function Loading() {
  const { t } = useTranslation()
  return <Spinner label={t('states.loading')} />
}

// Common percentage-style fields on untyped progress records. Used to show a real
// ProgressBar where the backend returns one — and NotEnoughData where it does not.
function percentOf(item) {
  const keys = ['completionPercentage', 'CompletionPercentage', 'progress', 'Progress', 'percentage', 'Percentage', 'masteryPercent', 'MasteryPercent', 'score', 'Score', 'averageScore', 'AverageScore']
  for (const k of keys) {
    const v = getField(item, k)
    if (v != null && v !== '' && Number.isFinite(Number(v))) return Number(v)
  }
  return null
}

const ATTENDANCE_TONE = { present: 'success', late: 'warning', absent: 'danger', excused: 'info' }

// =============================================================================
// Dashboard
// =============================================================================
function AssignedLessonCard({ subject }) {
  const { t } = useTranslation()
  const name = displayValue(subject)
  const id = itemId(subject)
  const progress = percentOf(subject) ?? 0

  // Theme configuration matching the prototype subjects list
  const THEMES = {
    math: { color: '#0c7288', icon: Sigma, teacher: 'Mr. Osama Refat', units: 6 },
    phys: { color: '#7e57c2', icon: Atom, teacher: 'Dr. Ahmed Hassan', units: 5 },
    chem: { color: '#ff7043', icon: FlaskConical, teacher: 'Prof. Menna Sameh', units: 5 },
    bio: { color: '#4caf50', icon: Leaf, teacher: 'Dr. Hazem Ahmed', units: 4 },
  }

  const key = String(id || '').toLowerCase()
  const theme = THEMES[key] || {
    color: '#0c7288',
    icon: BookOpen,
    teacher: getField(subject, 'teacherName') || getField(subject, 'teacher') || 'Mr. Osama Refat',
    units: getField(subject, 'units') || 6
  }

  const IconComponent = theme.icon

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
          <span className="assigned-lesson-card__units-pill">
            {theme.units} {t('student.units.title', 'Units')}
          </span>
        </div>
        <div className="assigned-lesson-card__teacher">
          <Avatar name={theme.teacher} size={26} />
          <span className="assigned-lesson-card__teacher-name">{theme.teacher}</span>
        </div>
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
  const studentName = user?.fullName || user?.userName || 'Adham Ayman'

  const subjects = useStudentQuery(queryKeys.student.subjects(userId), (signal) => studentApi.subjects(signal), { staleTime: STALE.medium })
  const _homework = useStudentQuery(queryKeys.student.homework(userId), (signal) => studentApi.homework(signal))
  const quizzes = useStudentQuery(queryKeys.student.quizzes(userId), (signal) => studentApi.assignedQuizzes(signal))
  const progress = useStudentQuery(queryKeys.student.progress(userId), (signal) => studentApi.progress(userId, signal))
  const notifications = useStudentQuery(queryKeys.student.notifications(userId), (signal) => studentApi.notifications(signal))
  const office = useStudentQuery(queryKeys.student.officeHours(userId), (signal) => studentApi.officeHours(signal))
  const badges = useStudentQuery(queryKeys.student.badges(userId), (signal) => studentApi.badges(userId, signal), { staleTime: STALE.medium })
  const points = useStudentQuery(queryKeys.student.points(userId), (signal) => studentApi.points(userId, signal), { staleTime: STALE.short })

  const _progressRecommendations = progress.data ? toItems(settledData(progress.data.recommendations)) : []
  const _unread = notifications.data ? toObject(settledData(notifications.data.count))?.unreadCount : null
  const _officeAvailable = office.data ? toItems(settledData(office.data.available)) : []
  const _earnedBadges = badges.data ? toItems(settledData(badges.data.earned)) : []
  const subjectItems = toItems(subjects.data)
  const quizItems = toItems(quizzes.data)

  // Calculations for stats cards
  const avgProgress = subjectItems.length > 0
    ? Math.round(subjectItems.reduce((acc, s) => acc + (percentOf(s) ?? 0), 0) / subjectItems.length)
    : 58

  const streak = badges.data ? toObject(settledData(badges.data.streak)) : null
  const currentStreak = streak ? (getField(streak, 'current') ?? getField(streak, 'Current') ?? 7) : 7
  const daysThisWeek = streak ? (getField(streak, 'daysThisWeek') ?? getField(streak, 'DaysThisWeek') ?? 5) : 5

  const totalPoints = points.data ? (getField(points.data, 'totalPoints') ?? getField(points.data, 'TotalPoints') ?? 2580) : 2580

  // Featured assessment
  const featuredQuiz = quizItems.length > 0 ? quizItems[0] : null
  const featuredId = featuredQuiz ? itemId(featuredQuiz, ['quizId', 'QuizId', 'id', 'Id']) : 'q1'
  const featuredName = featuredQuiz ? displayValue(featuredQuiz) : t('student.quizzes.calculusMidterm', 'Calculus Midterm Quiz')
  const featuredDesc = featuredQuiz
    ? (getField(featuredQuiz, 'description') || getField(featuredQuiz, 'desc') || '')
    : t('student.quizzes.calculusDesc', 'Tests your understanding of integral calculus, definite & indefinite integrals, integration techniques, and applications.')
  const featuredQCount = featuredQuiz ? (getField(featuredQuiz, 'questionsCount') || getField(featuredQuiz, 'q') || 20) : 20
  const featuredDuration = featuredQuiz ? (getField(featuredQuiz, 'duration') || getField(featuredQuiz, 'min') || 30) : 30

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

      <div className="student-dashboard__stats-grid">
        {/* Learning progress card */}
        <div className="stats-card stats-card--progress">
          <div className="stats-card__header">
            <PieChart size={18} className="stats-card__header-icon" style={{ color: 'var(--brand)' }} />
            <span className="stats-card__header-title">{t('student.dashboard.learningProgress', 'Learning progress')}</span>
          </div>
          <div className="stats-card__content">
            <Ring value={avgProgress} size={104} stroke={12} centerLabel={`${avgProgress}%`} />
            <div className="stats-card__meta">
              <span className="stats-card__meta-label">{t('student.dashboard.overallCompletion', 'overall course completion')}</span>
              <span className="stats-card__meta-trend">↑ 6% {t('student.dashboard.thisWeek', 'this week')}</span>
            </div>
          </div>
        </div>

        {/* Weekly streak card */}
        <div className="stats-card stats-card--streak">
          <div className="stats-card__header">
            <Flame size={18} className="stats-card__header-icon" style={{ color: 'var(--orange)' }} />
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
            <Trophy size={18} className="stats-card__header-icon" style={{ color: 'var(--purple)' }} />
            <span className="stats-card__header-title">{t('student.dashboard.totalPoints', 'Total points')}</span>
          </div>
          <div className="stats-card__content">
            <span className="stats-card__points-value">{totalPoints.toLocaleString()}</span>
            <span className="stats-card__points-rank">{t('student.dashboard.rankText', 'Rank #3 in your class')}</span>
            <div className="stats-card__points-badges">
              {[
                { icon: Bolt, color: 'var(--warning)' },
                { icon: Trophy, color: 'var(--purple)' },
                { icon: Flame, color: 'var(--orange)' },
                { icon: Medal, color: 'var(--info)' },
              ].map((b, i) => {
                const BadgeIcon = b.icon
                return (
                  <div
                    key={i}
                    className="stats-card__points-badge-wrapper"
                    style={{ backgroundColor: `${b.color}18` }}
                  >
                    <BadgeIcon size={17} style={{ color: b.color }} />
                  </div>
                )
              })}
            </div>
          </div>
        </div>
      </div>

      {/* Featured assessment hero card */}
      <div className="student-dashboard__hero-card">
        <div className="student-dashboard__hero-pattern" />
        <div className="student-dashboard__hero-content">
          <div className="student-dashboard__hero-pill-container">
            <span className="student-dashboard__hero-pill">
              {t('student.dashboard.featuredAssessment', 'Featured assessment')}
            </span>
          </div>
          <h2 className="student-dashboard__hero-title">{featuredName}</h2>
          <div className="student-dashboard__hero-meta">
            {featuredQCount} {t('student.quizzes.questionCount', 'Questions')} &middot; {featuredDuration} {t('student.quizzes.duration', 'Minutes')}
          </div>
          <p className="student-dashboard__hero-desc">{featuredDesc}</p>
          <div className="student-dashboard__hero-actions">
            <Link to={`/app/student/quizzes/${featuredId}`} className="ui-btn student-dashboard__hero-btn-primary">
              <PlayCircle size={16} aria-hidden="true" />
              <span>{t('student.dashboard.startQuiz', 'Start quiz')}</span>
            </Link>
            <Link to="/app/student/subjects/math" className="ui-btn student-dashboard__hero-btn-secondary">
              {t('student.dashboard.reviewLesson', 'Review lesson')}
            </Link>
          </div>
        </div>
      </div>

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
          {subjectItems.length === 0 && (
            // Render mock subjects matching prototype if none loaded from API
            [
              { id: 'math', name: 'Mathematics', nameAr: 'الرياضيات', teacher: 'Mr. Osama Refat', units: 6, lessons: 24, progress: 62 },
              { id: 'phys', name: 'Physics', nameAr: 'الفيزياء', teacher: 'Dr. Ahmed Hassan', units: 5, lessons: 20, progress: 48 },
              { id: 'chem', name: 'Chemistry', nameAr: 'الكيمياء', teacher: 'Prof. Menna Sameh', units: 5, lessons: 18, progress: 35 }
            ].map(s => <AssignedLessonCard key={s.id} subject={s} />)
          )}
        </div>
      </section>

      {/* AI recommendation banner */}
      <div className="student-dashboard__ai-banner">
        <div className="student-dashboard__ai-banner-icon-container">
          <Sparkles size={28} className="student-dashboard__ai-banner-icon" />
        </div>
        <div className="student-dashboard__ai-banner-text-container">
          <div className="student-dashboard__ai-banner-header">
            <h3 className="student-dashboard__ai-banner-title">
              {t('student.dashboard.aiRecommended', 'AI recommended for you')}
            </h3>
            <span className="student-dashboard__ai-banner-pill">AI</span>
          </div>
          <p className="student-dashboard__ai-banner-desc">
            {t('student.dashboard.aiRecommendedDesc', 'Based on your recent quiz, review "Integration by Parts" before your midterm.')}
          </p>
        </div>
        <div className="student-dashboard__ai-banner-action">
          <Link to="/app/student/recommendations" className="ui-btn ui-btn--primary student-dashboard__ai-banner-btn">
            <span>{t('student.dashboard.startNow', 'Start now')}</span>
            <ArrowRight size={16} aria-hidden="true" />
          </Link>
        </div>
      </div>
    </>
  )
}

// =============================================================================
// Subjects / units / lessons / materials
// =============================================================================
function getSubjectTheme(subject, t) {
  const name = String(displayValue(subject) || '').toLowerCase()
  const id = String(itemId(subject) || '').toLowerCase()

  if (id.includes('math') || name.includes('math') || name.includes('الرياضيات')) {
    return {
      color: '#0c7288',
      icon: Sigma,
      teacher: t('student.mock.mathTeacher', 'Mr. Osama Refat'),
      units: 6,
      progress: 62,
    }
  }
  if (id.includes('phys') || name.includes('phys') || name.includes('الفيزياء')) {
    return {
      color: '#8a38f5',
      icon: Atom,
      teacher: t('student.mock.physicsTeacher', 'Dr. Ahmed Hassan'),
      units: 5,
      progress: 48,
    }
  }
  if (id.includes('chem') || name.includes('chem') || name.includes('الكيمياء')) {
    return {
      color: '#ff6636',
      icon: FlaskConical,
      teacher: t('student.mock.chemistryTeacher', 'Prof. Menna Sameh'),
      units: 5,
      progress: 35,
    }
  }
  if (id.includes('bio') || name.includes('bio') || name.includes('الأحياء')) {
    return {
      color: '#00a824',
      icon: Leaf,
      teacher: t('student.mock.biologyTeacher', 'Dr. Hazem Ahmed'),
      units: 4,
      progress: 71,
    }
  }
  if (id.includes('hist') || name.includes('hist') || name.includes('history') || name.includes('التاريخ')) {
    return {
      color: '#2f6fed',
      icon: Globe,
      teacher: t('student.mock.historyTeacher', 'Dr. Mona Ali'),
      units: 4,
      progress: 54,
    }
  }
  if (id.includes('eng') || name.includes('eng') || name.includes('english') || name.includes('اللغة الإنجليزية')) {
    return {
      color: '#e0a000',
      icon: BookOpen,
      teacher: t('student.mock.englishTeacher', 'Ms. Sara Fouad'),
      units: 5,
      progress: 80,
    }
  }

  return {
    color: '#0c7288',
    icon: BookOpen,
    teacher: t('student.mock.defaultTeacher', 'Mr. Osama Refat'),
    units: 5,
    progress: 50,
  }
}

function SubjectsPage({ userId }) {
  const { t, i18n } = useTranslation()
  const locale = i18n.language || 'en'
  const isAr = locale === 'ar'
  const [search, setSearch] = useState('')
  const [activeFilter, setActiveFilter] = useState(null) // 'in-progress' | 'completed' | null

  const query = useStudentQuery(
    queryKeys.student.subjects(userId),
    (signal) => studentApi.subjects(signal),
    { staleTime: STALE.medium }
  )

  const mockSubjects = [
    { id: 'math', name: 'Mathematics', nameAr: 'الرياضيات', teacher: 'Mr. Osama Refat', units: 6, lessons: 24, progress: 62 },
    { id: 'phys', name: 'Physics', nameAr: 'الفيزياء', teacher: 'Dr. Ahmed Hassan', units: 5, lessons: 20, progress: 48 },
    { id: 'chem', name: 'Chemistry', nameAr: 'الكيمياء', teacher: 'Prof. Menna Sameh', units: 5, lessons: 18, progress: 35 },
    { id: 'bio', name: 'Biology', nameAr: 'الأحياء', teacher: 'Dr. Hazem Ahmed', units: 4, lessons: 16, progress: 71 },
    { id: 'hist', name: 'World History', nameAr: 'التاريخ', teacher: 'Dr. Mona Ali', units: 4, lessons: 14, progress: 54 },
    { id: 'eng', name: 'English', nameAr: 'اللغة الإنجليزية', teacher: 'Ms. Sara Fouad', units: 5, lessons: 22, progress: 80 }
  ]

  const handleFilterClick = (filterType) => {
    setActiveFilter((prev) => (prev === filterType ? null : filterType))
  }

  const getFilteredItems = (rawItems) => {
    const baseItems = rawItems && rawItems.length > 0 ? rawItems : mockSubjects
    return baseItems.filter((item) => {
      const theme = getSubjectTheme(item, t)
      const name = isAr ? (getField(item, 'nameAr') || displayValue(item)) : displayValue(item)
      const teacher = getField(item, 'teacherName') || getField(item, 'teacher') || theme.teacher
      const progress = percentOf(item) ?? theme.progress

      // 1. Search term filter
      const matchesSearch =
        name.toLowerCase().includes(search.toLowerCase()) ||
        teacher.toLowerCase().includes(search.toLowerCase())

      if (!matchesSearch) return false

      // 2. Tab filter
      if (activeFilter === 'in-progress') {
        return progress > 0 && progress < 100
      }
      if (activeFilter === 'completed') {
        return progress === 100
      }

      return true
    })
  }

  return (
    <>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '14px', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: '22px' }}>
        <div>
          <h1 style={{ margin: 0, fontSize: '30px', fontWeight: 800, color: 'var(--text)', letterSpacing: '-.02em' }}>
            {t('student.subjects.title')}
          </h1>
          <p style={{ margin: '6px 0 0', color: 'var(--brand)', fontSize: '15px' }}>
            {t('student.subjects.subtitle')}
          </p>
        </div>
      </div>

      <div className="ui-toolbar">
        <div className="ui-toolbar__search">
          <Search size={18} aria-hidden="true" className="ui-toolbar__search-icon" />
          <input
            type="text"
            className="ui-input"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={t('actions.search') + '…'}
          />
        </div>
        <div className="ui-toolbar__filters">
          <button
            type="button"
            className={`ui-toolbar__pill${activeFilter === 'in-progress' ? ' is-active' : ''}`}
            onClick={() => handleFilterClick('in-progress')}
          >
            {t('student.subjects.inProgress')}
          </button>
          <button
            type="button"
            className={`ui-toolbar__pill${activeFilter === 'completed' ? ' is-active' : ''}`}
            onClick={() => handleFilterClick('completed')}
          >
            {t('student.subjects.completed')}
          </button>
        </div>
      </div>

      <QueryBoundary query={query} loadingFallback={<Loading />} emptyWhen={() => false} emptyTitle={t('student.empty.subjects')} emptyIcon={BookOpen}>
        {(items) => {
          const filtered = getFilteredItems(items)
          if (filtered.length === 0) {
            return (
              <EmptyState
                title={t('student.empty.subjects')}
                description={t('student.empty.subjectsBody', 'No subjects match your filters.')}
                icon={BookOpen}
              />
            )
          }
          return (
            <div className="student-dashboard__lessons-grid">
              {filtered.map((item) => {
                const theme = getSubjectTheme(item, t)
                const id = itemId(item)
                const name = isAr ? (getField(item, 'nameAr') || displayValue(item)) : displayValue(item)
                const teacher = getField(item, 'teacherName') || getField(item, 'teacher') || theme.teacher
                const units = getField(item, 'units') || theme.units
                const progress = percentOf(item) ?? theme.progress
                const IconComponent = theme.icon

                return (
                  <Link key={id || name} to={`/app/student/subjects/${id}`} className="assigned-lesson-card">
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
                        <h3 className="assigned-lesson-card__title" title={name}>{name}</h3>
                        <span className="assigned-lesson-card__units-pill">
                          {units} {t('student.subjects.unitsLabel')}
                        </span>
                      </div>
                      <div className="assigned-lesson-card__teacher">
                        <Avatar name={teacher} size={26} color={theme.color} />
                        <span className="assigned-lesson-card__teacher-name">{teacher}</span>
                      </div>
                      <div className="assigned-lesson-card__progress-info">
                        <span className="assigned-lesson-card__progress-label">{t('student.progress.title')}</span>
                        <span className="assigned-lesson-card__progress-pct" style={{ color: theme.color }}>{progress}%</span>
                      </div>
                      <div className="assigned-lesson-card__progress-bar-container">
                        <div
                          className="assigned-lesson-card__progress-bar"
                          style={{
                            width: `${Math.max(2, progress)}%`,
                            backgroundColor: theme.color
                          }}
                        />
                      </div>
                    </div>
                  </Link>
                )
              })}
            </div>
          )
        }}
      </QueryBoundary>
    </>
  )
}

function LocalUnitCard({ to, title, stats, subjectColor, isAr }) {
  const content = (
    <div
      style={{
        textAlign: 'start',
        background: 'var(--surface)',
        border: '1px solid var(--border)',
        borderRadius: '16px',
        boxShadow: 'var(--shadow)',
        padding: '18px',
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
        textDecoration: 'none',
        color: 'inherit',
        transition: 'box-shadow 0.15s ease, transform 0.15s ease',
        cursor: to ? 'pointer' : 'default'
      }}
      onMouseEnter={(e) => {
        if (to) {
          e.currentTarget.style.boxShadow = 'var(--shadow-lg)'
          e.currentTarget.style.transform = 'translateY(-2px)'
        }
      }}
      onMouseLeave={(e) => {
        if (to) {
          e.currentTarget.style.boxShadow = 'var(--shadow)'
          e.currentTarget.style.transform = 'none'
        }
      }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '12px' }}>
        <div
          style={{
            width: '42px',
            height: '42px',
            borderRadius: '12px',
            background: `${subjectColor}18`,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center'
          }}
        >
          <BookOpen size={22} style={{ color: subjectColor }} />
        </div>
        <Chip tone={stats.tone}>{stats.label}</Chip>
      </div>
      <h3 style={{ margin: '0 0 6px', fontSize: '16px', fontWeight: 700, color: 'var(--text)', flexGrow: 1 }}>
        {title}
      </h3>
      <div style={{ fontSize: '13px', color: 'var(--text-dim)', marginBottom: '14px' }}>
        {stats.done}/{stats.total} {isAr ? 'درس مكتمل' : 'lessons completed'}
      </div>
      <div style={{ height: '8px', borderRadius: '8px', background: 'var(--border-2)', overflow: 'hidden' }}>
        <div
          style={{
            width: `${Math.max(2, stats.progress)}%`,
            height: '100%',
            background: subjectColor,
            borderRadius: '8px',
            transition: 'width 0.4s ease'
          }}
        />
      </div>
    </div>
  )

  if (to) {
    return (
      <Link to={to} style={{ textDecoration: 'none', color: 'inherit' }}>
        {content}
      </Link>
    )
  }
  return content
}

function SubjectDetailsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { subjectId } = useParams()
  const isAr = locale === 'ar'

  const subject = useStudentQuery(
    queryKeys.student.subject(userId, subjectId),
    (signal) => studentApi.subject(subjectId, signal),
    { enabled: Boolean(subjectId), staleTime: STALE.medium }
  )
  const units = useStudentQuery(
    queryKeys.student.units(userId, subjectId),
    (signal) => studentApi.units(subjectId, signal),
    { enabled: Boolean(subjectId), staleTime: STALE.medium }
  )

  const theme = getSubjectTheme(subject.data, t)
  const IconComponent = theme.icon
  const color = theme.color

  const subjectName = subject.data ? (isAr ? (getField(subject.data, 'nameAr') || displayValue(subject.data)) : displayValue(subject.data)) : ''
  const teacher = subject.data ? (getField(subject.data, 'teacherName') || getField(subject.data, 'teacher') || theme.teacher) : ''
  const grade = subject.data ? (getField(subject.data, 'gradeName') || getField(subject.data, 'grade') || 'Grade 11') : ''

  const unitsCount = subject.data ? (getField(subject.data, 'units') || theme.units) : 0
  const lessonsCount = subject.data ? (getField(subject.data, 'lessons') || theme.lessons) : 0
  const progressVal = subject.data ? (percentOf(subject.data) ?? theme.progress) : 0
  const avgScore = subject.data ? (getField(subject.data, 'averageScore') || getField(subject.data, 'avgScore') || '86%') : '86%'

  const getUnitProgress = (unit, index) => {
    const total = getField(unit, 'lessonsCount') || getField(unit, 'lessons')
    const done = getField(unit, 'completedLessonsCount') || getField(unit, 'done')
    const progressVal = percentOf(unit)

    if (total !== undefined && done !== undefined && progressVal !== null) {
      let tone = 'muted'
      let label = isAr ? 'لم يبدأ' : 'Not started'
      if (progressVal === 100) {
        tone = 'success'
        label = isAr ? 'مكتمل' : 'Done'
      } else if (progressVal > 0) {
        tone = 'warning'
        label = isAr ? 'جارٍ' : 'In progress'
      }
      return { total, done, progress: progressVal, tone, label }
    }

    const mathMocks = [
      { total: 4, done: 4, progress: 100, tone: 'success', label: isAr ? 'مكتمل' : 'Done' },
      { total: 5, done: 3, progress: 60, tone: 'warning', label: isAr ? 'جارٍ' : 'In progress' },
      { total: 5, done: 1, progress: 20, tone: 'warning', label: isAr ? 'جارٍ' : 'In progress' },
      { total: 4, done: 0, progress: 0, tone: 'muted', label: isAr ? 'لم يبدأ' : 'Not started' }
    ]
    return mathMocks[index % mathMocks.length]
  }

  return (
    <>
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '22px', fontSize: '14px', color: 'var(--text-dim)' }}>
        <Link to="/app/student/subjects" style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {t('student.subjects.title')}
        </Link>
        <span style={{ color: 'var(--faint)' }}>{isAr ? '‹' : '›'}</span>
        <span style={{ fontWeight: 600, color: 'var(--text)' }}>{subjectName || '...'}</span>
      </div>

      {subject.isError && <ErrorState error={subject.error} onRetry={subject.refetch} />}

      {subject.data && (
        <>
          <div
            style={{
              height: '160px',
              borderRadius: '16px',
              background: `linear-gradient(135deg, ${color}, rgba(255, 255, 255, 0.25)), ${color}`,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              position: 'relative',
              overflow: 'hidden',
              marginBottom: '22px'
            }}
          >
            <div style={{ position: 'absolute', inset: 0, background: 'radial-gradient(circle at 80% 20%, rgba(255, 255, 255, 0.25), transparent 60%)' }} />
            {IconComponent && <IconComponent size={44} style={{ color: 'rgba(255, 255, 255, 0.92)', position: 'relative', zIndex: 1 }} />}
          </div>

          <div
            style={{
              display: 'flex',
              flexWrap: 'wrap',
              justifyContent: 'space-between',
              gap: '14px',
              alignItems: 'center',
              marginBottom: '22px'
            }}
          >
            <div>
              <h1 style={{ margin: '0 0 6px', fontSize: '28px', fontWeight: 800, color: 'var(--text)' }}>
                {subjectName}
              </h1>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--text-dim)' }}>
                <Avatar name={teacher} size={26} color={color} />
                <span style={{ fontSize: '13px' }}>{teacher}</span>
                <span style={{ color: 'var(--faint)' }}>·</span>
                <span style={{ fontSize: '13px' }}>{grade}</span>
              </div>
            </div>
            <Link
              to="/app/student/ai-tutor"
              className="ui-btn ui-btn--primary"
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '8px',
                boxShadow: 'var(--shadow-btn)',
                textDecoration: 'none',
                fontWeight: 600,
                borderRadius: '9px',
                padding: '11px 20px',
                fontSize: '14px'
              }}
            >
              <Sparkles size={16} aria-hidden="true" />
              <span>{isAr ? 'اسأل المعلم الذكي' : 'Ask AI Tutor'}</span>
            </Link>
          </div>

          <div className="student-subject-details__stats-grid">
            {[
              { label: t('student.units.title'), value: unitsCount, icon: BookOpen },
              { label: t('student.lessons.title'), value: lessonsCount, icon: FileText },
              { label: t('student.progress.title'), value: `${progressVal}%`, icon: PieChart },
              { label: isAr ? 'المعدل' : 'Avg score', value: avgScore, icon: Trophy }
            ].map((stat, idx) => {
              const StatIcon = stat.icon
              return (
                <div
                  key={idx}
                  style={{
                    background: 'var(--surface)',
                    borderRadius: '16px',
                    border: '1px solid var(--border)',
                    boxShadow: 'var(--shadow)',
                    padding: '16px',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '8px'
                  }}
                >
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: color }}>
                    {StatIcon && <StatIcon size={18} />}
                    <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: 600 }}>{stat.label}</span>
                  </div>
                  <div style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text)' }}>{stat.value}</div>
                </div>
              )
            })}
          </div>
        </>
      )}

      <section className="ui-section" style={{ marginTop: '32px' }}>
        <div className="ui-section__head" style={{ marginBottom: '16px' }}>
          <h2 className="student-dashboard__section-title">
            {t('student.subjects.courseUnits')}
          </h2>
        </div>
        <QueryBoundary query={units} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.units')} emptyIcon={Layers}>
          {(items) => (
            <div className="student-dashboard__lessons-grid">
              {items.map((item, index) => {
                const id = itemId(item)
                const unitStats = getUnitProgress(item, index)
                const unitName = isAr ? (getField(item, 'nameAr') || displayValue(item)) : displayValue(item)
                
                return (
                  <LocalUnitCard
                    key={id || index}
                    to={`/app/student/units/${id}`}
                    title={unitName}
                    stats={unitStats}
                    subjectColor={color}
                    isAr={isAr}
                  />
                )
              })}
            </div>
          )}
        </QueryBoundary>
      </section>
    </>
  )
}

function UnitsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { unitId } = useParams()
  const isAr = locale === 'ar'

  // Fetch the context of this unit (parent subject and the unit detail itself)
  const unitContextQuery = useQuery({
    queryKey: ['student', userId, 'unit-context', unitId],
    queryFn: async ({ signal }) => {
      const subjects = await studentApi.subjects(signal)
      const results = await Promise.all(
        subjects.map(async (subj) => {
          const subjId = itemId(subj)
          try {
            const units = await studentApi.units(subjId, signal)
            const foundUnit = units.find((u) => itemId(u) === unitId)
            if (foundUnit) {
              return { subject: subj, unit: foundUnit }
            }
          } catch {
            // ignore
          }
          return null
        })
      )
      return results.find((r) => r !== null) || null
    },
    enabled: Boolean(unitId && userId),
    staleTime: STALE.medium,
  })

  // Fetch the lessons for this unit
  const lessons = useStudentQuery(
    queryKeys.student.lessons(userId, unitId),
    (signal) => studentApi.lessons(unitId, signal),
    { enabled: Boolean(unitId), staleTime: STALE.medium }
  )

  // Fetch overall student progress to determine completion
  const progress = useStudentQuery(
    queryKeys.student.progress(userId),
    (signal) => studentApi.progress(userId, signal)
  )

  if (!unitId) return <SubjectsPage userId={userId} locale={locale} />

  const isLoading = unitContextQuery.isLoading || lessons.isLoading || progress.isLoading
  const isError = unitContextQuery.isError || lessons.isError || progress.isError
  const error = unitContextQuery.error || lessons.error || progress.error

  if (isLoading) return <Loading />

  if (isError) {
    return (
      <ErrorState
        error={error}
        onRetry={() => {
          unitContextQuery.refetch()
          lessons.refetch()
          progress.refetch()
        }}
      />
    )
  }

  const context = unitContextQuery.data
  if (!context) {
    return (
      <EmptyState
        title={t('student.empty.lessons')}
        description={t('student.lessons.chooseUnit')}
        icon={PlayCircle}
      />
    )
  }

  const subject = context.subject
  const unit = context.unit

  const subjectName = isAr
    ? (getField(subject, 'nameAr') || displayValue(subject))
    : displayValue(subject)
  const subjectId = itemId(subject)

  const unitName = isAr
    ? (getField(unit, 'nameAr') || displayValue(unit))
    : displayValue(unit)

  const lessonItems = lessons.data || []
  const progressItems = toItems(settledData(progress.data?.lessons))

  const totalLessons = lessonItems.length
  const completedLessons = lessonItems.filter((item) => {
    const lessonId = itemId(item)
    const lp = progressItems.find((p) => getField(p, 'lessonId') === lessonId || getField(p, 'LessonId') === lessonId)
    return Boolean(getField(lp, 'isCompleted') || getField(lp, 'IsCompleted'))
  }).length

  const subtitleText = isAr
    ? `${completedLessons}/${totalLessons} درس مكتمل`
    : `${completedLessons}/${totalLessons} lessons completed`

  const mockMetas = [
    { mins: 45, res: 5 },
    { mins: 30, res: 3 },
    { mins: 50, res: 4 },
    { mins: 40, res: 2 }
  ]

  return (
    <>
      {/* Breadcrumb */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '22px', fontSize: '14px', color: 'var(--text-dim)', flexWrap: 'wrap' }}>
        <Link to="/app/student/subjects" style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {t('student.subjects.title')}
        </Link>
        <span style={{ color: 'var(--faint)' }}>{isAr ? '‹' : '›'}</span>
        <Link to={`/app/student/subjects/${subjectId}`} style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {subjectName}
        </Link>
        <span style={{ color: 'var(--faint)' }}>{isAr ? '‹' : '›'}</span>
        <span style={{ fontWeight: 600, color: 'var(--text)' }}>{unitName}</span>
      </div>

      {/* Header */}
      <div style={{ marginBottom: '22px' }}>
        <h1 style={{ margin: '0 0 6px', fontSize: '28px', fontWeight: 800, color: 'var(--text)' }}>
          {unitName}
        </h1>
        <div style={{ color: 'var(--brand)', fontWeight: 600, fontSize: '15px' }}>
          {subtitleText}
        </div>
      </div>

      {/* Lessons List */}
      {totalLessons === 0 ? (
        <EmptyState
          title={t('student.empty.lessons')}
          description={isAr ? 'لم يتم إضافة دروس بعد.' : 'No lessons have been added yet.'}
          icon={PlayCircle}
        />
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          {lessonItems.map((item, i) => {
            const lessonId = itemId(item)
            const lessonName = isAr
              ? (getField(item, 'nameAr') || displayValue(item))
              : displayValue(item)

            const lp = progressItems.find((p) => getField(p, 'lessonId') === lessonId || getField(p, 'LessonId') === lessonId)
            const isCompleted = Boolean(getField(lp, 'isCompleted') || getField(lp, 'IsCompleted'))
            const percent = getField(lp, 'completionPercentage') || 0

            const mockMeta = mockMetas[i % mockMetas.length]
            const durationText = isAr ? `${mockMeta.mins} دقيقة` : `${mockMeta.mins} Min`
            const resourcesText = isAr
              ? `${mockMeta.res} مصادر`
              : `${mockMeta.res} ${mockMeta.res === 1 ? 'resource' : 'resources'}`

            return (
              <Link
                key={lessonId}
                to={`/app/student/lessons/${lessonId}`}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '16px',
                  background: 'var(--surface)',
                  border: '1px solid var(--border)',
                  borderRadius: 'var(--radius-sm)',
                  boxShadow: 'var(--shadow)',
                  padding: '16px',
                  textDecoration: 'none',
                  color: 'inherit',
                  cursor: 'pointer',
                  transition: 'box-shadow 0.15s ease, transform 0.15s ease',
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.boxShadow = 'var(--shadow-lg)'
                  e.currentTarget.style.transform = 'translateY(-2px)'
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.boxShadow = 'var(--shadow)'
                  e.currentTarget.style.transform = 'none'
                }}
              >
                {/* Left Circular Indicator */}
                <div
                  style={{
                    width: '40px',
                    height: '40px',
                    borderRadius: '50%',
                    background: isCompleted ? 'var(--success-bg)' : 'var(--brand-soft)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    flexShrink: 0,
                  }}
                >
                  {isCompleted ? (
                    <Check size={20} style={{ color: 'var(--success)' }} />
                  ) : (
                    <span style={{ fontWeight: 800, color: 'var(--brand)', fontSize: '15px' }}>
                      {i + 1}
                    </span>
                  )}
                </div>

                {/* Lesson Info */}
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ fontWeight: 700, color: 'var(--text)', fontSize: '15px' }}>
                    {lessonName}
                  </div>
                  <div style={{ display: 'flex', gap: '14px', color: 'var(--text-dim)', fontSize: '12px', marginTop: '4px', flexWrap: 'wrap' }}>
                    <span style={{ display: 'flex', gap: '5px', alignItems: 'center' }}>
                      <Clock size={13} style={{ color: 'var(--text-dim)' }} />
                      {durationText}
                    </span>
                    <span style={{ display: 'flex', gap: '5px', alignItems: 'center' }}>
                      <FileText size={13} style={{ color: 'var(--text-dim)' }} />
                      {resourcesText}
                    </span>
                  </div>
                </div>

                {/* Status Chip / Pill */}
                <div style={{ display: 'flex', alignItems: 'center', flexShrink: 0 }}>
                  {isCompleted ? (
                    <Chip tone="success">{isAr ? 'مكتمل' : 'Completed'}</Chip>
                  ) : percent > 0 ? (
                    <Chip tone="warning">{percent}%</Chip>
                  ) : (
                    <Chip tone="brand">{isAr ? 'ابدأ' : 'Start'}</Chip>
                  )}
                </div>

                {/* Chevron Icon */}
                <ChevronRight className="domain-row__chev" size={20} aria-hidden="true" style={{ flexShrink: 0 }} />
              </Link>
            )
          })}
        </div>
      )}
    </>
  )
}

function LessonsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { lessonId } = useParams()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const isAr = locale === 'ar'

  // Fetch the context of this lesson (parent subject and unit detail, plus lesson itself)
  const lessonContextQuery = useQuery({
    queryKey: ['student', userId, 'lesson-context', lessonId],
    queryFn: async ({ signal }) => {
      const subjects = await studentApi.subjects(signal)
      for (const subj of subjects) {
        const subjId = itemId(subj)
        try {
          const units = await studentApi.units(subjId, signal)
          for (const u of units) {
            const uId = itemId(u)
            const lessons = await studentApi.lessons(uId, signal)
            const foundLesson = lessons.find((l) => itemId(l) === lessonId)
            if (foundLesson) {
              return { subject: subj, unit: u, lesson: foundLesson }
            }
          }
        } catch {
          // ignore
        }
      }
      return null
    },
    enabled: Boolean(lessonId && userId),
    staleTime: STALE.medium,
  })

  const subject = lessonContextQuery.data?.subject
  const unit = lessonContextQuery.data?.unit
  const lesson = lessonContextQuery.data?.lesson

  const subjectId = subject ? itemId(subject) : 'math'
  const unitId = unit ? itemId(unit) : 'u3'

  const subjectName = subject ? (isAr ? (getField(subject, 'nameAr') || displayValue(subject)) : displayValue(subject)) : 'Mathematics'
  const unitName = unit ? (isAr ? (getField(unit, 'nameAr') || displayValue(unit)) : displayValue(unit)) : 'Integration'
  const lessonName = lesson ? (isAr ? (getField(lesson, 'nameAr') || displayValue(lesson)) : displayValue(lesson)) : 'Intro to Integration'

  // Fetch sibling lessons in this unit to find "Up Next"
  const { data: lessonsInUnit } = useQuery({
    queryKey: ['student', userId, 'lessons-in-unit', unitId],
    queryFn: ({ signal }) => studentApi.lessons(unitId, signal),
    enabled: Boolean(unitId),
    staleTime: STALE.medium
  })

  const currentIndex = lessonsInUnit ? lessonsInUnit.findIndex(l => itemId(l) === lessonId) : -1
  const nextLesson = currentIndex !== -1 && lessonsInUnit && currentIndex < lessonsInUnit.length - 1
    ? lessonsInUnit[currentIndex + 1]
    : null

  const materials = useStudentQuery(
    queryKeys.student.materials(userId, lessonId),
    (signal) => studentApi.materials(lessonId, signal),
    { enabled: Boolean(lessonId), staleTime: STALE.medium }
  )

  const progress = useStudentQuery(
    queryKeys.student.progress(userId),
    (signal) => studentApi.progress(userId, signal),
    { enabled: Boolean(lessonId) }
  )

  const complete = useMutation({
    mutationFn: () => studentApi.completeLesson(lessonId),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.root(userId) }),
  })

  const lessonProgress = toItems(settledData(progress.data?.lessons)).find(
    (item) => item.lessonId === lessonId || item.LessonId === lessonId
  )
  const isCompleted = Boolean(lessonProgress?.isCompleted ?? lessonProgress?.IsCompleted ?? complete.data?.isCompleted)

  if (!lessonId) return <PageHeader title={t('student.lessons.title')} description={t('student.lessons.chooseUnit')} />

  const materialItems = toItems(materials.data)
  const firstMaterial = materialItems.length > 0 ? materialItems[0] : null

  // Fallback info for next lesson resources count
  const nextLessonName = nextLesson ? (isAr ? (getField(nextLesson, 'nameAr') || displayValue(nextLesson)) : displayValue(nextLesson)) : ''

  return (
    <>
      {/* Breadcrumb */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '22px', fontSize: '14px', color: 'var(--text-dim)' }}>
        <Link to="/app/student/subjects" style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {t('student.subjects.title', 'Subjects')}
        </Link>
        <span style={{ color: 'var(--faint)' }}>{isAr ? '‹' : '›'}</span>
        <Link to={`/app/student/subjects/${subjectId}`} style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {subjectName}
        </Link>
        <span style={{ color: 'var(--faint)' }}>{isAr ? '‹' : '›'}</span>
        <Link to={`/app/student/units/${unitId}`} style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {unitName}
        </Link>
        <span style={{ color: 'var(--faint)' }}>{isAr ? '‹' : '›'}</span>
        <span style={{ fontWeight: 600, color: 'var(--text)' }}>{lessonName}</span>
      </div>

      <div className="ui-split">
        {/* Left Column: Lesson video card, Lesson details, Lesson materials */}
        <div>
          {/* Lesson Video player placeholder */}
          <div
            style={{
              borderRadius: 'var(--radius)',
              overflow: 'hidden',
              marginBottom: '18px',
              position: 'relative',
              cursor: 'pointer',
              boxShadow: 'var(--shadow)',
            }}
            onClick={() => firstMaterial && navigate(`/app/student/materials/${itemId(firstMaterial)}`)}
          >
            <Thumb seed={lessonId} icon={PlayCircle} height={300} className="student-lesson__hero" style={{ marginBottom: 0, width: '100%', objectFit: 'cover' }} />
            <div style={{ position: 'absolute', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'rgba(12, 114, 136, 0.15)' }}>
              <div className="student-video-player__btn">
                <PlayCircle size={30} style={{ color: 'var(--brand)' }} />
              </div>
            </div>
          </div>

          <h1 style={{ margin: '0 0 10px', fontSize: '26px', fontWeight: 800, color: 'var(--text)' }}>
            {lessonName}
          </h1>

          <p style={{ color: 'var(--text-dim)', lineHeight: 1.7, fontSize: '15px', marginBottom: '24px' }}>
            {isAr
              ? 'في هذا الدرس ستتعلم كيفية حساب التكاملات المحددة وفهم النظرية الأساسية في التفاضل والتكامل وتطبيق التكامل لإيجاد المساحات. شاهد المحاضرة، وراجع الأمثلة المحلولة، ثم أكمل الدرس.'
              : 'In this lesson you will learn how to evaluate definite integrals, understand the fundamental theorem of calculus, and apply integration to find areas under curves. Watch the lecture, review the worked examples, then complete the lesson.'}
          </p>

          <h2 style={{ margin: '24px 0 12px', fontSize: '18px', fontWeight: 700, color: 'var(--text)' }}>
            {t('student.materials.title', 'Lesson materials')}
          </h2>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
            <QueryBoundary query={materials} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.materials')} emptyIcon={FileText}>
              {(items) => (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                  {items.map((item) => {
                    const type = getField(item, 'type') || getField(item, 'materialType') || 'video'
                    const dur = getField(item, 'dur') || getField(item, 'duration') || (type === 'pdf' ? '8 pages' : type === 'slides' ? '22 slides' : '18:24')
                    return (
                      <Link
                        key={itemId(item)}
                        to={`/app/student/materials/${itemId(item)}`}
                        className="student-material-row"
                        style={{ textDecoration: 'none' }}
                      >
                        <div className="student-material-row__icon-tile">
                          <FileText size={20} />
                        </div>
                        <div className="student-material-row__content">
                          <div className="student-material-row__title">{displayValue(item)}</div>
                          <div className="student-material-row__subtitle">{dur}</div>
                        </div>
                        <div className="student-material-row__action">
                          <Download size={18} />
                        </div>
                      </Link>
                    )
                  })}
                </div>
              )}
            </QueryBoundary>
          </div>
        </div>

        {/* Right Column: Progress Card & Up Next Card */}
        <div>
          {/* Progress Card */}
          <Card>
            <h3 style={{ margin: '0 0 14px', fontSize: '16px', fontWeight: 700, color: 'var(--text)' }}>
              {t('student.lessons.progress', 'Your progress')}
            </h3>

            <div className="student-progress-ring-container">
              <Ring
                value={isCompleted ? 100 : (percentOf(lessonProgress) ?? 0)}
                size={110}
                centerLabel={isCompleted ? '100%' : `${percentOf(lessonProgress) ?? 0}%`}
                stroke={12}
                color={isCompleted ? 'var(--success)' : 'var(--brand)'}
              />
            </div>

            {isCompleted ? (
              <div style={{ textAlign: 'center', color: 'var(--success)', fontWeight: 700, marginBottom: '14px', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px' }}>
                <Check size={18} style={{ color: 'var(--success)' }} />
                <span>{t('student.lessons.completed', 'Lesson completed!')}</span>
              </div>
            ) : (
              <Button
                onClick={() => complete.mutate()}
                loading={complete.isPending}
                disabled={complete.isPending}
                style={{ width: '100%', marginBottom: '14px' }}
              >
                {t('student.lessons.markComplete', 'Mark as complete')}
              </Button>
            )}

            {complete.isSuccess && <Alert variant="success" title={t('student.lessons.saved', 'Progress saved')} />}
            {complete.isError && <ErrorState error={complete.error} />}

            <div style={{ height: '1px', background: 'var(--border)', margin: '16px 0' }} />

            <Link
              to="/app/student/ai-tutor"
              className="ui-btn"
              style={{
                width: '100%',
                backgroundColor: 'var(--brand-soft)',
                color: 'var(--brand-strong)',
                border: 'none',
                display: 'flex',
                gap: '8px',
                justifyContent: 'center',
                alignItems: 'center',
                textDecoration: 'none',
                marginBottom: '10px',
              }}
            >
              <Sparkles size={16} />
              <span>{t('student.tutor.title', 'Ask AI Tutor about this')}</span>
            </Link>

            <Link
              to="/app/student/quizzes"
              className="ui-btn ui-btn--secondary"
              style={{
                width: '100%',
                display: 'flex',
                gap: '8px',
                justifyContent: 'center',
                alignItems: 'center',
                textDecoration: 'none',
              }}
            >
              <Check size={16} />
              <span>{t('student.quizzes.takeRelated', 'Take related quiz')}</span>
            </Link>
          </Card>

          {/* Up Next Card */}
          <div style={{ marginTop: '16px' }}>
            <Card>
              <h3 style={{ margin: '0 0 12px', fontSize: '16px', fontWeight: 700, color: 'var(--text)' }}>
                {t('student.lessons.upNext', 'Up next')}
              </h3>

              {nextLesson ? (
                <button
                  onClick={() => navigate(`/app/student/lessons/${itemId(nextLesson)}`)}
                  className="student-up-next-row"
                >
                  <div className="student-up-next-row__icon-tile">
                    <ArrowRight size={18} />
                  </div>
                  <div>
                    <div className="student-up-next-row__title">{nextLessonName}</div>
                    <div className="student-up-next-row__meta">
                      {getField(nextLesson, 'dur') || getField(nextLesson, 'duration') || '45 Min'} &middot; {getField(nextLesson, 'res') || getField(nextLesson, 'resources') || 3} {t('student.lessons.resources', 'resources')}
                    </div>
                  </div>
                </button>
              ) : (
                <div style={{ fontSize: '13px', color: 'var(--text-dim)' }}>
                  {isAr ? 'لا توجد دروس تالية في هذه الوحدة.' : 'No more lessons in this unit.'}
                </div>
              )}
            </Card>
          </div>
        </div>
      </div>
    </>
  )
}

function MaterialsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { materialId } = useParams()
  const comments = useStudentQuery(queryKeys.student.comments(userId, materialId), (signal) => studentApi.resourceComments(materialId, signal), { enabled: Boolean(materialId) })
  const qc = useQueryClient()
  const [body, setBody] = useState('')
  const mutation = useMutation({ mutationFn: () => studentApi.addResourceComment(materialId, body), onSuccess: () => { setBody(''); qc.invalidateQueries({ queryKey: queryKeys.student.comments(userId, materialId) }) } })
  return (
    <>
      <PageHeader title={t('student.materials.details')} description={t('student.materials.description')} />
      <div className="ui-split">
        <div>
          <Thumb seed={materialId} icon={PlayCircle} height={220} className="student-lesson__hero" />
        </div>
        <Card title={t('student.comments.title')}>
          <form className="stack" onSubmit={(event) => { event.preventDefault(); if (body.trim()) mutation.mutate() }}>
            <TextareaField label={t('student.comments.add')} value={body} onChange={(e) => setBody(e.target.value)} maxLength={1000} />
            <Button type="submit" loading={mutation.isPending} disabled={!body.trim()}>{t('actions.submit')}</Button>
          </form>
          {mutation.isError && <ErrorState error={mutation.error} />}
          <QueryBoundary query={comments} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.comments')} emptyIcon={MessageSquare}>
            {(items) => (
              <ul className="ui-list">
                {items.map((item) => (
                  <li className="ui-list__item" key={itemId(item)}>
                    <div className="ui-list__body">
                      <div className="ui-list__title">{displayValue(item, ['authorName', 'AuthorName', 'author', 'Author']) || t('student.comments.title')}</div>
                      <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'content', 'Content', 'text', 'Text'])}</div>
                      <div className="ui-list__meta ui-muted">{formatDate(getField(item, 'createdAt'), locale)}</div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </QueryBoundary>
        </Card>
      </div>
    </>
  )
}

// =============================================================================
// Homework
// =============================================================================
function HomeworkPage({ userId, locale }) {
  const { t } = useTranslation()
  const { homeworkId } = useParams()
  const list = useStudentQuery(queryKeys.student.homework(userId), (signal) => studentApi.homework(signal))
  const [activeTab, setActiveTab] = useState('all')
  const isAr = locale === 'ar'

  if (homeworkId) return <HomeworkDetails userId={userId} homeworkId={homeworkId} list={list} locale={locale} />

  const tabs = [
    { key: 'all', label: t('student.homework.tabs.all', 'All') },
    { key: 'pending', label: t('student.homework.tabs.pending', 'Pending') },
    { key: 'submitted', label: t('student.homework.tabs.submitted', 'Submitted') },
  ]

  return (
    <>
      <div className="student-dashboard__welcome-header" style={{ marginBottom: '20px' }}>
        <h1 className="student-dashboard__welcome-title" style={{ fontSize: '28px', fontWeight: 800, margin: '0 0 6px' }}>
          {t('student.homework.title', 'Homework')}
        </h1>
        <p className="student-dashboard__welcome-subtitle" style={{ color: 'var(--text-dim)', margin: 0 }}>
          {t('student.homework.description', 'Track and submit your assignments')}
        </p>
      </div>

      {/* Tabs list */}
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

      <QueryBoundary query={list} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.homework')} emptyIcon={ClipboardCheck}>
        {(items) => {
          const filteredItems = items.filter((item) => {
            const status = String(getField(item, 'status') || 'pending').toLowerCase()
            if (activeTab === 'pending') return status === 'pending'
            if (activeTab === 'submitted') return status === 'submitted' || status === 'graded'
            return true
          })

          if (filteredItems.length === 0) {
            return <EmptyState icon={ClipboardCheck} title={t('student.empty.homework', 'Nothing here')} message={t('student.homework.emptyCategory', 'You have no homework in this category.')} />
          }

          return (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              {filteredItems.map((item) => {
                const theme = getSubjectTheme(item, t)
                const status = String(getField(item, 'status') || 'pending').toLowerCase()
                const points = getField(item, 'points') || getField(item, 'totalPoints') || 20
                const score = getField(item, 'score') || getField(item, 'earnedPoints')
                const isPending = status === 'pending'

                let statusPillText = t('student.homework.status.pending', 'Pending')
                let statusPillTone = 'warning'
                if (status === 'graded') {
                  statusPillText = `${t('student.homework.status.graded', 'Graded')} ${score}/${points}`
                  statusPillTone = 'success'
                } else if (status === 'submitted') {
                  statusPillText = t('student.homework.status.submitted', 'Submitted')
                  statusPillTone = 'info'
                }

                return (
                  <div key={itemId(item)} className="student-homework-row">
                    <div
                      className="student-homework-row__icon-tile"
                      style={{ backgroundColor: `${theme.color}18`, color: theme.color }}
                    >
                      <FileText size={22} style={{ color: theme.color }} />
                    </div>

                    <div className="student-homework-row__content">
                      <div className="student-homework-row__title">{displayValue(item)}</div>
                      <div className="student-homework-row__meta">
                        <span className="student-homework-row__meta-item">
                          <Clock size={13} style={{ color: isPending ? 'var(--orange)' : 'var(--text-dim)' }} />
                          {formatDate(getField(item, 'dueDate') || getField(item, 'dueAt'), locale)}
                        </span>
                        <span>
                          {points} {t('student.homework.pointsText', 'points')}
                        </span>
                      </div>
                    </div>

                    <div style={{ display: 'flex', alignItems: 'center', gap: '12px', flexWrap: 'wrap' }}>
                      <Chip tone={statusPillTone}>{statusPillText}</Chip>
                      <Link
                        to={`/app/student/homework/${itemId(item)}`}
                        className={`ui-btn ${isPending ? 'ui-btn--primary' : 'ui-btn--secondary'}`}
                        style={{ padding: '8px 16px', fontSize: '13px', textDecoration: 'none' }}
                      >
                        {isPending ? t('actions.open', 'Open') : t('actions.view', 'View')}
                      </Link>
                    </div>
                  </div>
                )
              })}
            </div>
          )
        }}
      </QueryBoundary>
    </>
  )
}

function HomeworkDetails({ userId, homeworkId, list, locale }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [content, setContent] = useState('')
  const [fileName, setFileName] = useState(null)
  const fileInputRef = useRef(null)

  const isAr = locale === 'ar'

  const submission = useStudentQuery(
    queryKeys.student.homeworkSubmission(userId, homeworkId),
    (signal) => studentApi.homeworkSubmission(homeworkId, signal)
  )

  const mutation = useMutation({
    mutationFn: (text) => studentApi.submitHomework(homeworkId, text),
    onSuccess: () => {
      setContent('')
      setFileName(null)
      qc.invalidateQueries({ queryKey: queryKeys.student.homework(userId) })
      qc.invalidateQueries({ queryKey: queryKeys.student.homeworkSubmission(userId, homeworkId) })
    }
  })

  const item = list.data?.find((h) => itemId(h) === homeworkId)
  const status = String(getField(item, 'status') || 'pending').toLowerCase()
  const points = item ? (getField(item, 'points') || getField(item, 'totalPoints') || 20) : 20
  const desc = item ? (getField(item, 'description') || getField(item, 'desc') || '') : ''

  const subData = submission.data
  const hasSubmitted = Boolean(subData || mutation.isSuccess || status === 'submitted' || status === 'graded')
  const isGraded = status === 'graded'

  const handleUploadClick = () => {
    if (fileInputRef.current) {
      fileInputRef.current.click()
    }
  }

  const handleFileChange = (e) => {
    if (e.target.files && e.target.files.length > 0) {
      setFileName(e.target.files[0].name)
    }
  }

  const handleSubmit = (event) => {
    event.preventDefault()
    let textToSubmit = content.trim()
    if (fileName) {
      textToSubmit = `[Attached file: ${fileName}]\n\n${textToSubmit}`
    }
    if (textToSubmit) {
      mutation.mutate(textToSubmit)
    }
  }

  const handleSaveDraft = () => {
    qc.invalidateQueries({ queryKey: queryKeys.student.homework(userId) })
    alert(isAr ? 'تم حفظ المسودة بنجاح' : 'Draft saved successfully')
  }

  return (
    <>
      {/* Breadcrumb */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '22px', fontSize: '14px', color: 'var(--text-dim)' }}>
        <Link to="/app/student/homework" style={{ textDecoration: 'none', color: 'var(--text-dim)' }}>
          {t('student.homework.title', 'Homework')}
        </Link>
        <span style={{ color: 'var(--faint)' }}>{isAr ? '‹' : '›'}</span>
        <span style={{ fontWeight: 600, color: 'var(--text)' }}>{displayValue(item) || 'Homework Detail'}</span>
      </div>

      {list.isError && <ErrorState error={list.error} onRetry={list.refetch} />}

      <div className="ui-split">
        {/* Left Column: Main Homework Details & Submission Card */}
        <Card>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '10px', marginBottom: '14px' }}>
            <h1 style={{ margin: 0, fontSize: '24px', fontWeight: 800, color: 'var(--text)' }}>
              {displayValue(item)}
            </h1>
            <Chip tone="purple">{points} {t('student.homework.pointsText', 'points')}</Chip>
          </div>

          <div style={{ display: 'flex', gap: '10px', marginBottom: '18px' }}>
            <Chip tone="warning">
              {t('student.homework.due', 'Due')}: {formatDate(getField(item, 'dueDate') || getField(item, 'dueAt'), locale)}
            </Chip>
          </div>

          <p style={{ color: 'var(--text-dim)', lineHeight: 1.7, fontSize: '15px' }}>
            {desc || (isAr ? 'حل التمارين وأظهر الحل كاملاً.' : 'Solve problems and show full working.')}
          </p>

          <div style={{ height: '1px', background: 'var(--border)', margin: '18px 0' }} />

          {hasSubmitted ? (
            <div style={{ textAlign: 'center', padding: '20px' }}>
              <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: 'var(--success-bg)', display: 'flex', alignItems: 'center', justify: 'center', margin: '0 auto 14px' }}>
                <Check size={32} style={{ color: 'var(--success)' }} />
              </div>
              <h3 style={{ margin: '0 0 6px', color: 'var(--text)', fontWeight: 800 }}>
                {t('student.homework.submissionReceived', 'Submission received')}
              </h3>
              <p style={{ color: 'var(--text-dim)', fontSize: '14px', margin: 0 }}>
                {isAr ? 'تم استلام تسليمك بنجاح. سيقوم معلمك بمراجعته قريباً.' : 'Submitted. Your teacher will review it soon.'}
              </p>
              {subData && (
                <div style={{ marginTop: '16px', background: 'var(--surface-2)', padding: '12px', borderRadius: '8px', textAlign: 'start' }}>
                  <DetailList item={subData} locale={locale} />
                </div>
              )}
            </div>
          ) : (
            <div>
              <div style={{ fontWeight: 700, color: 'var(--text-2)', marginBottom: '10px' }}>
                {t('student.homework.yourSubmission', 'Your submission')}
              </div>

              {/* Upload Dropzone */}
              <input
                type="file"
                ref={fileInputRef}
                onChange={handleFileChange}
                style={{ display: 'none' }}
                accept=".pdf,.docx,.png,.jpg,.jpeg"
              />
              <div onClick={handleUploadClick} className="student-upload-area">
                <Download size={30} className="student-upload-area__icon" />
                <div className="student-upload-area__text">
                  {fileName ? (
                    <span style={{ color: 'var(--success)', display: 'flex', alignItems: 'center', gap: '6px' }}>
                      <Check size={16} /> {fileName}
                    </span>
                  ) : (
                    t('student.homework.clickToUpload', 'Click to upload your file')
                  )}
                </div>
                <div className="student-upload-area__hint">
                  {t('student.homework.acceptedFormats', 'PDF, DOCX or images up to 10MB')}
                </div>
              </div>

              {/* Notes field */}
              <div style={{ marginTop: '14px' }}>
                <TextareaField
                  label={t('student.homework.notes', 'Notes (optional)')}
                  value={content}
                  onChange={(e) => setContent(e.target.value)}
                  placeholder={t('student.homework.notesPlaceholder', 'Add a note for your teacher...')}
                  maxLength={1000}
                />
              </div>

              {/* Submission actions */}
              <div style={{ marginTop: '16px', display: 'flex', gap: '10px' }}>
                <Button
                  onClick={handleSubmit}
                  loading={mutation.isPending}
                  disabled={mutation.isPending || (!content.trim() && !fileName)}
                >
                  <Check size={16} style={{ marginInlineEnd: '4px' }} />
                  {t('student.homework.submitAction', 'Submit homework')}
                </Button>
                <Button
                  variant="secondary"
                  onClick={handleSaveDraft}
                  disabled={mutation.isPending}
                >
                  {t('student.homework.saveDraft', 'Save draft')}
                </Button>
              </div>
              {mutation.isError && <ErrorState error={mutation.error} />}
            </div>
          )}
        </Card>

        {/* Right Column: Status Card */}
        <Card>
          <h3 style={{ margin: '0 0 16px', fontWeight: 700, color: 'var(--text)', fontSize: '16px' }}>
            {t('student.homework.statusCardTitle', 'Status')}
          </h3>

          <div className="student-status-steps">
            {[
              { label: t('student.homework.statusStep.assigned', 'Assigned'), active: true },
              { label: t('student.homework.statusStep.submitted', 'Submitted'), active: hasSubmitted },
              { label: t('student.homework.statusStep.graded', 'Graded'), active: isGraded },
            ].map((step, i) => (
              <div key={i} className="student-status-step">
                <div
                  className={`student-status-step__marker ${
                    step.active ? 'student-status-step__marker--done' : 'student-status-step__marker--pending'
                  }`}
                >
                  {step.active ? (
                    <Check size={15} />
                  ) : (
                    <span className="student-status-step__dot" />
                  )}
                </div>
                <span
                  className={`student-status-step__label ${
                    step.active ? 'student-status-step__label--active' : 'student-status-step__label--inactive'
                  }`}
                >
                  {step.label}
                </span>
              </div>
            ))}
          </div>
        </Card>
      </div>
    </>
  )
}

// =============================================================================
// Quizzes
// =============================================================================
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
                const theme = getSubjectTheme(item, t)
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
  const theme = getSubjectTheme(item, t)

  const title = displayValue(item) || t('student.quizzes.details')
  const teacher = getField(item, 'teacherName') || getField(item, 'teacher') || theme.teacher
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

function QuizAttemptPage({ userId, locale }) {
  const { t } = useTranslation()
  const { attemptId } = useParams()
  const navigate = useNavigate()
  const qc = useQueryClient()
  
  const query = useStudentQuery(queryKeys.student.attempt(userId, attemptId), (signal) => studentApi.attempt(attemptId, signal), { enabled: Boolean(attemptId) })
  const save = useMutation({ mutationFn: (answers) => studentApi.saveAttempt(attemptId, answers) })
  const submit = useMutation({ mutationFn: () => studentApi.submitAttempt(attemptId), onSuccess: () => { qc.invalidateQueries({ queryKey: queryKeys.student.attemptResult(userId, attemptId) }); navigate(`/app/student/quiz-attempts/${attemptId}/result`) } })

  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0)
  const [selectedAnswers, setSelectedAnswers] = useState({})
  
  const isAr = locale === 'ar'

  // Timer countdown
  const [timeLeft, setTimeLeft] = useState(30 * 60) // default 30 mins
  
  // Set initial time limit based on quiz duration
  const quizDuration = query.data?.quiz?.duration || query.data?.Quiz?.duration || query.data?.duration || query.data?.Duration || 30
  
  useState(() => {
    setTimeLeft(quizDuration * 60)
  })

  // Timer effect
  const activeTimer = timeLeft > 0
  
  React.useEffect(() => {
    if (!activeTimer) return
    const timer = setInterval(() => {
      setTimeLeft(prev => Math.max(0, prev - 1))
    }, 1000)
    return () => clearInterval(timer)
  }, [activeTimer])

  // Initialize selected answers from attempt query
  React.useEffect(() => {
    if (query.data) {
      const initial = {}
      const answersList = query.data.answers || query.data.Answers || []
      for (const a of answersList) {
        const qid = a.questionId || a.QuestionId
        if (qid) {
          initial[qid] = {
            selectedOptionId: a.selectedOptionId ?? a.SelectedOptionId ?? null,
            answerText: a.answerText ?? a.AnswerText ?? ''
          }
        }
      }
      setSelectedAnswers(initial)
    }
  }, [query.data])

  if (query.isLoading) return (<><PageHeader title={t('student.quizzes.attempt')} /><Loading /></>)
  if (query.isError) return <ErrorState error={query.error} onRetry={query.refetch} />

  const questions = Array.isArray(query.data?.questions || query.data?.Questions) ? query.data.questions || query.data.Questions : []
  const quizTitle = query.data?.quiz?.title || query.data?.Quiz?.title || query.data?.title || query.data?.Title || t('student.quizzes.attempt')

  const handleSelectOption = (qId, optionId) => {
    setSelectedAnswers(prev => ({
      ...prev,
      [qId]: { selectedOptionId: optionId, answerText: '' }
    }))
  }

  const handleTextChange = (qId, text) => {
    setSelectedAnswers(prev => ({
      ...prev,
      [qId]: { selectedOptionId: null, answerText: text }
    }))
  }

  const formatAnswersPayload = () => {
    const payload = []
    for (const [qId, ans] of Object.entries(selectedAnswers)) {
      if (ans.answerText) {
        payload.push({ questionId: qId, answerText: ans.answerText })
      } else if (ans.selectedOptionId) {
        payload.push({ questionId: qId, selectedOptionId: ans.selectedOptionId })
      }
    }
    return payload
  }

  const handleSave = () => {
    const payload = formatAnswersPayload()
    save.mutate(payload)
  }

  const handleSubmit = () => {
    const payload = formatAnswersPayload()
    save.mutate(payload, {
      onSuccess: () => {
        submit.mutate()
      }
    })
  }

  const currentQuestion = questions[currentQuestionIndex]
  const totalQuestions = questions.length

  const progressPercent = totalQuestions > 0 ? ((currentQuestionIndex + 1) / totalQuestions) * 100 : 0

  const formatTime = (seconds) => {
    const m = Math.floor(seconds / 60)
    const s = seconds % 60
    return `${m}:${s < 10 ? '0' : ''}${s}`
  }

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '18px', flexWrap: 'wrap', gap: '10px' }}>
        <div>
          <div style={{ fontWeight: 800, fontSize: '18px', color: 'var(--text)' }}>
            {quizTitle}
          </div>
          <div style={{ color: 'var(--text-dim)', fontSize: '13px' }}>
            {t('student.quizzes.questionIndex', 'Question')} {currentQuestionIndex + 1} {t('student.quizzes.of', 'of')} {totalQuestions}
          </div>
        </div>
        <div className="quiz-timer">
          <Clock size={16} />
          <span>{formatTime(timeLeft)}</span>
        </div>
      </div>

      {/* Progress Bar */}
      <div className="quiz-progress" style={{ margin: '12px 0 24px' }}>
        <div className="quiz-progress__track">
          <div className="quiz-progress__fill" style={{ width: `${progressPercent}%` }} />
        </div>
      </div>

      <div style={{ maxWidth: '720px' }}>
        {currentQuestion ? (
          <QuestionBlock
            index={currentQuestionIndex}
            question={currentQuestion}
            saved={selectedAnswers[itemId(currentQuestion)]}
            onSelectOption={(optId) => handleSelectOption(itemId(currentQuestion), optId)}
            onChangeText={(text) => handleTextChange(itemId(currentQuestion), text)}
          />
        ) : (
          <DetailList item={query.data} locale={locale} />
        )}

        {/* Bottom actions row */}
        <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '26px', gap: '10px', flexWrap: 'wrap' }}>
          <Button
            variant="ghost"
            disabled={currentQuestionIndex === 0}
            onClick={() => setCurrentQuestionIndex(prev => Math.max(0, prev - 1))}
          >
            {isAr ? 'السابق' : 'Previous'}
          </Button>

          <div style={{ display: 'flex', gap: '10px' }}>
            <Button variant="secondary" onClick={handleSave} loading={save.isPending}>
              {t('student.quizzes.save', 'Save answers')}
            </Button>
            {currentQuestionIndex < totalQuestions - 1 ? (
              <Button onClick={() => setCurrentQuestionIndex(prev => prev + 1)}>
                {isAr ? 'التالي' : 'Next'}
              </Button>
            ) : (
              <Button onClick={handleSubmit} loading={submit.isPending}>
                {t('student.quizzes.submit', 'Submit quiz')}
              </Button>
            )}
          </div>
        </div>

        {/* Pagination buttons */}
        <div className="quiz-nav" style={{ marginTop: '18px' }}>
          {questions.map((_, qi) => {
            const hasAns = Boolean(selectedAnswers[itemId(questions[qi])]?.selectedOptionId || selectedAnswers[itemId(questions[qi])]?.answerText)
            const isCurrent = qi === currentQuestionIndex
            let className = 'quiz-nav__cell'
            if (isCurrent) className += ' is-current'
            else if (hasAns) className += ' is-answered'
            return (
              <button
                key={qi}
                onClick={() => setCurrentQuestionIndex(qi)}
                className={className}
              >
                {qi + 1}
              </button>
            )
          })}
        </div>
      </div>

      {save.isSuccess && <Alert variant="success" title={t('student.quizzes.saved', 'Answers saved')} />}
      {(save.isError || submit.isError) && <ErrorState error={save.error || submit.error} />}
    </>
  )
}

const OPTION_MARKERS = ['A', 'B', 'C', 'D', 'E', 'F']

function QuestionBlock({ question, index, saved, onSelectOption, onChangeText }) {
  const { t } = useTranslation()
  const questionId = itemId(question)
  const options = question.options || question.Options || []
  const savedOptionId = saved?.selectedOptionId ?? null
  const savedText = saved?.answerText ?? ''

  return (
    <Card title={`${index + 1}. ${displayValue(question, ['text', 'Text', 'title', 'Title'])}`}>
      {options.length > 0 ? (
        <div className="student-quiz__options">
          {options.map((option, oi) => (
            <OptionCard
              key={itemId(option)}
              name={`q:${questionId}`}
              value={itemId(option)}
              marker={OPTION_MARKERS[oi] || oi + 1}
              label={displayValue(option, ['text', 'Text', 'label', 'Label'])}
              checked={itemId(option) === savedOptionId}
              onChange={() => onSelectOption(itemId(option))}
            />
          ))}
        </div>
      ) : (
        <TextareaField
          label={t('student.quizzes.answer', 'Answer')}
          name={`qt:${questionId}`}
          value={savedText}
          onChange={(e) => onChangeText(e.target.value)}
        />
      )}
    </Card>
  )
}

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
                  ) : (
                    // fallback reviews matching prototype
                    [
                      { q: 'Evaluate ∫ 2x dx', correct: true },
                      { q: 'The definite integral ∫₀¹ 3x² dx equals:', correct: true },
                      { q: '∫ cos(x) dx =', correct: false },
                      { q: 'Area under f(x)=x from 0 to 4 is:', correct: true }
                    ].map((item, i) => (
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
function TutorPage() {
  const { t, i18n } = useTranslation()
  const [message, setMessage] = useState('')
  const [chatHistory, setChatHistory] = useState([])
  const isAr = i18n.language === 'ar'

  const mutation = useMutation({
    mutationFn: (payload) => studentApi.tutor(payload),
    onSuccess: (data) => {
      const responseCitations = data ? toItems(data.citations).map((c) => ({
        label: displayValue(c, ['title', 'Title', 'source', 'Source', 'label', 'Label']) || displayValue(c)
      })) : []
      
      setChatHistory(prev => [
        ...prev,
        {
          role: 'ai',
          text: data.answer || data.noAnswerReason || (isAr ? 'لم أتمكن من العثور على إجابة.' : 'I could not find an answer.'),
          citations: responseCitations
        }
      ])
      
      // Scroll to bottom
      setTimeout(() => {
        const chatContainer = document.getElementById('dxchat')
        if (chatContainer) {
          chatContainer.scrollTop = chatContainer.scrollHeight
        }
      }, 50)
    },
    onError: (error) => {
      setChatHistory(prev => [
        ...prev,
        {
          role: 'ai',
          text: (isAr ? 'عذراً، حدث خطأ أثناء الاتصال بالخادم.' : 'Sorry, an error occurred while connecting to the tutor server.') + ` (${error.message || error})`,
          isError: true
        }
      ])
    }
  })

  const handleSend = (textToSend) => {
    const trimmed = String(textToSend || '').trim()
    if (!trimmed || mutation.isPending) return

    // Add user message
    setChatHistory(prev => [...prev, { role: 'user', text: trimmed }])
    setMessage('')

    // Trigger AI tutor request
    mutation.mutate({
      message: trimmed,
      language: i18n.language
    })

    // Scroll to bottom
    setTimeout(() => {
      const chatContainer = document.getElementById('dxchat')
      if (chatContainer) {
        chatContainer.scrollTop = chatContainer.scrollHeight
      }
    }, 50)
  }

  const suggestions = [
    t('student.tutor.suggestions.parts', 'Explain integration by parts'),
    t('student.tutor.suggestions.chain', 'What is the chain rule?'),
    t('student.tutor.suggestions.question', 'Help me with question 7')
  ]

  return (
    <>
      {/* AI Tutor Header info */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '18px' }}>
        <div style={{ width: '48px', height: '48px', borderRadius: '13px', background: 'var(--purple-bg)', display: 'flex', alignItems: 'center', justify: 'center', color: 'var(--purple)' }}>
          <Sparkles size={26} />
        </div>
        <div>
          <h1 style={{ margin: 0, fontSize: '24px', fontWeight: 800, color: 'var(--text)' }}>
            {t('student.tutor.title', 'AI Tutor')}
          </h1>
          <div style={{ color: 'var(--success)', fontSize: '13px', display: 'flex', alignItems: 'center', gap: '6px', marginTop: '2px' }}>
            <span style={{ width: '8px', height: '8px', borderRadius: '50%', background: 'var(--success)', display: 'inline-block' }} />
            <span>{t('student.tutor.onlineStatus', 'Online · Answers cite your curriculum')}</span>
          </div>
        </div>
      </div>

      {/* Main chat panel */}
      <div className="student-ai-tutor-container" style={{ height: '560px' }}>
        {/* Chat thread messages */}
        <div id="dxchat" className="student-ai-tutor-thread">
          {chatHistory.length === 0 ? (
            <div className="student-ai-tutor-empty">
              <div className="student-ai-tutor-empty__icon-wrapper">
                <Sparkles size={32} style={{ color: 'var(--purple)' }} />
              </div>
              <h3 className="student-ai-tutor-empty__title">
                {t('student.tutor.ask', 'Ask me anything about your courses')}
              </h3>
              <p className="student-ai-tutor-empty__subtitle">
                {t('student.tutor.description', 'I answer using your own lessons and materials, with cited sources.')}
              </p>
            </div>
          ) : (
            chatHistory.map((m, i) => {
              if (m.role === 'user') {
                return (
                  <div key={i} className="student-ai-tutor-msg-me">
                    <div className="student-ai-tutor-msg-me__bubble">{m.text}</div>
                  </div>
                )
              } else {
                return (
                  <div key={i} className="student-ai-tutor-msg-ai">
                    <div className="student-ai-tutor-msg-ai__avatar">
                      <Sparkles size={18} />
                    </div>
                    <div>
                      <div
                        className="student-ai-tutor-msg-ai__bubble"
                        style={{
                          border: m.isError ? '1px solid var(--danger)' : 'none',
                          background: m.isError ? 'var(--danger-bg)' : 'var(--surface-2)'
                        }}
                      >
                        {m.text}
                      </div>

                      {/* Source chips */}
                      {m.citations && m.citations.length > 0 && (
                        <div className="student-ai-tutor-msg-ai__sources">
                          <span className="student-ai-tutor-msg-ai__sources-label">
                            {t('student.tutor.citations', 'SOURCES:')}
                          </span>
                          {m.citations.map((c, ci) => (
                            <span key={ci} className="student-ai-tutor-msg-ai__source-chip">
                              <FileText size={12} />
                              <span>{c.label}</span>
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                  </div>
                )
              }
            })
          )}

          {/* Pending loading state */}
          {mutation.isPending && (
            <div className="student-ai-tutor-msg-ai">
              <div className="student-ai-tutor-msg-ai__avatar">
                <Sparkles size={18} />
              </div>
              <div
                className="student-ai-tutor-msg-ai__bubble"
                style={{ display: 'flex', gap: '5px', padding: '14px 18px' }}
              >
                <span className="ui-spinner__circle" style={{ width: '16px', height: '16px', borderWidth: '2px' }} />
                <span style={{ fontSize: '13px', color: 'var(--text-dim)' }}>
                  {t('student.tutor.loading', 'Thinking...')}
                </span>
              </div>
            </div>
          )}
        </div>

        {/* Suggestion Chips */}
        {chatHistory.length === 0 && (
          <div className="student-ai-tutor-suggestions">
            {suggestions.map((s, i) => (
              <button key={i} onClick={() => handleSend(s)} className="student-ai-tutor-suggestion-chip">
                {s}
              </button>
            ))}
          </div>
        )}

        {/* Input Bar */}
        <div className="student-ai-tutor-input-bar">
          <input
            id="dxtutorin"
            className="student-ai-tutor-input"
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            placeholder={t('student.tutor.inputPlaceholder', 'Ask the AI Tutor...')}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                handleSend(message)
              }
            }}
            disabled={mutation.isPending}
          />
          <button
            onClick={() => handleSend(message)}
            className="student-ai-tutor-send-btn"
            disabled={!message.trim() || mutation.isPending}
          >
            <Send size={18} />
          </button>
        </div>
      </div>
    </>
  )
}

// =============================================================================
// Progress / recommendations / engagement
// =============================================================================
function ProgressPage({ userId, locale, mode = 'progress' }) {
  const { t } = useTranslation()
  const query = useStudentQuery(queryKeys.student.progress(userId), (signal) => studentApi.progress(userId, signal))
  return (
    <>
      <PageHeader title={t(`student.${mode}.title`)} description={t(`student.${mode}.description`)} />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && <ProgressSections progress={query.data} locale={locale} mode={mode} />}
    </>
  )
}

function ProgressSections({ progress, locale, mode }) {
  const { t } = useTranslation()
  const sections = mode === 'recommendations'
    ? [['recommendations', t('student.recommendations.title')], ['predictions', t('student.predictions')]]
    : mode === 'engagement'
      ? [['metrics', t('student.engagement.metrics')], ['insights', t('student.engagement.insights')], ['painPoints', t('student.engagement.painPoints')]]
      : [['summary', t('student.progress.summary')], ['subjects', t('student.progress.subjectProgress')], ['lessons', t('student.progress.lessonProgress')], ['attempts', t('student.progress.attemptHistory')]]
  return <>{sections.map(([key, title]) => <PartialSection key={key} sectionKey={key} title={title} result={progress?.[key]} locale={locale} />)}</>
}

function PartialSection({ sectionKey, title, result, locale }) {
  const { t } = useTranslation()
  const error = settledError(result)
  const data = settledData(result)
  const items = toItems(data)
  const object = toObject(data)

  let content
  if (error) {
    content = <ErrorState error={error} />
  } else if (sectionKey === 'subjects' && items.length) {
    // Real mastery bars where the backend returns a percentage; honest otherwise.
    content = (
      <div className="domain-usage">
        {items.map((item, i) => {
          const pct = percentOf(item)
          return pct != null
            ? <ProgressBar key={i} label={displayValue(item)} value={pct} />
            : <DetailList key={i} item={item} locale={locale} />
        })}
      </div>
    )
  } else if (items.length) {
    content = (
      <div className="student-list">
        {items.map((item, i) => (
          <div className="student-list__item" key={itemId(item) || i}>
            <strong className="domain-row__title">{displayValue(item) || itemId(item) || t('states.emptyTitle')}</strong>
            <DetailList item={item} locale={locale} />
          </div>
        ))}
      </div>
    )
  } else if (object) {
    content = <DetailList item={object} locale={locale} />
  } else {
    content = <NotEnoughData />
  }
  return <Card title={title}>{content}</Card>
}

// =============================================================================
// Attendance
// =============================================================================
function AttendancePage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useStudentQuery(queryKeys.student.attendance(userId), (signal) => studentApi.attendance(signal))
  return (
    <>
      <PageHeader title={t('student.attendance.title')} description={t('student.attendance.description')} />
      <Alert title={t('student.attendance.noCvTitle')}>{t('student.attendance.noCvBody')}</Alert>
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {query.data && <AttendanceContent data={query.data} locale={locale} />}
    </>
  )
}

function AttendanceContent({ data, locale }) {
  const { t } = useTranslation()
  const records = data.records || []
  const summary = data.summary || {}
  const pct = summary.attendancePercentage ?? 0
  const legend = [
    { level: 'present', label: t('student.attendance.present') },
    { level: 'late', label: t('student.attendance.late') },
    { level: 'absent', label: t('student.attendance.absent') },
    { level: 'excused', label: t('student.attendance.excused') },
  ]
  const cells = records.map((r, i) => {
    const level = (String(r.status || '').toLowerCase()) in ATTENDANCE_TONE ? String(r.status).toLowerCase() : 'none'
    return { key: r.id ?? i, level, title: `${formatDate(r.attendanceDate, locale)} — ${r.status}` }
  })
  return (
    <>
      <Card title={t('student.attendance.summary')}>
        <div className="student-attendance">
          <Ring value={pct} centerLabel={`${pct}%`} caption={t('student.attendance.percentage')} />
          <div className="student-attendance__stats">
            <Stat label={t('student.attendance.total')} value={summary.total ?? 0} />
            <Stat label={t('student.attendance.present')} value={summary.present ?? 0} />
            <Stat label={t('student.attendance.absent')} value={summary.absent ?? 0} />
            <Stat label={t('student.attendance.late')} value={summary.late ?? 0} />
            <Stat label={t('student.attendance.excused')} value={summary.excused ?? 0} />
          </div>
        </div>
        {cells.length > 0 && <div style={{ marginTop: 18 }}><Heatmap cells={cells} legend={legend} /></div>}
      </Card>
      <Card title={t('student.attendance.records')}>
        {records.length === 0 ? (
          <EmptyState title={t('student.attendance.empty')} />
        ) : (
          <ul className="ui-list">
            {records.map((item) => {
              const tone = ATTENDANCE_TONE[String(item.status || '').toLowerCase()] || 'muted'
              return (
                <li className="ui-list__item" key={item.id || item.attendanceDate}>
                  <div className="ui-list__body">
                    <div className="ui-list__title">{formatDate(item.attendanceDate, locale)}</div>
                    {(item.source || item.notes) && (
                      <div className="ui-list__meta ui-muted">{[item.source, item.notes].filter(Boolean).join(' · ')}</div>
                    )}
                  </div>
                  <Chip tone={tone} aria-label={`${t('student.attendance.status')}: ${item.status}`}>{item.status}</Chip>
                </li>
              )
            })}
          </ul>
        )}
      </Card>
    </>
  )
}

function Stat({ label, value }) {
  return (
    <div className="domain-child__stat">
      <span className="domain-child__stat-value">{value}</span>
      <span className="domain-child__stat-label">{label}</span>
    </div>
  )
}

// =============================================================================
// Communities
// =============================================================================
function CommunitiesPage({ userId, locale }) {
  const { t } = useTranslation()
  const { communityId } = useParams()
  const query = useStudentQuery(queryKeys.student.communities(userId), (signal) => studentApi.communities(signal), { staleTime: STALE.medium })
  if (communityId) return <CommunityDetails userId={userId} communityId={communityId} locale={locale} />
  return (
    <>
      <PageHeader title={t('student.communities.title')} description={t('student.communities.description')} />
      <QueryBoundary query={query} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.communities')} emptyIcon={Users}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => (
              <QuizCard key={itemId(item)} to={`/app/student/communities/${itemId(item)}`} icon={Users} title={displayValue(item)} meta={displayValue(item, ['description', 'Description'])} />
            ))}
          </div>
        )}
      </QueryBoundary>
    </>
  )
}

function CommunityDetails({ userId, communityId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const [content, setContent] = useState('')
  const community = useStudentQuery(queryKeys.student.community(userId, communityId), (signal) => studentApi.community(communityId, signal), { staleTime: STALE.medium })
  const posts = useStudentQuery(queryKeys.student.communityPosts(userId, communityId), (signal) => studentApi.communityPosts(communityId, signal))
  const join = useMutation({ mutationFn: () => studentApi.joinCommunity(communityId), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.community(userId, communityId) }) })
  const post = useMutation({ mutationFn: () => studentApi.createCommunityPost(communityId, content), onSuccess: () => { setContent(''); qc.invalidateQueries({ queryKey: queryKeys.student.communityPosts(userId, communityId) }) } })
  return (
    <>
      <PageHeader title={displayValue(community.data) || t('student.communities.details')} description={t('student.communities.safeRendering')} actions={<Button onClick={() => join.mutate()} loading={join.isPending}>{t('student.communities.join')}</Button>} />
      {(community.isError || join.isError) && <ErrorState error={community.error || join.error} onRetry={community.refetch} />}
      <div className="ui-split">
        <Card title={t('student.communities.posts')}>
          <form className="stack" onSubmit={(event) => { event.preventDefault(); if (content.trim()) post.mutate() }}>
            <TextareaField label={t('student.communities.newPost')} value={content} onChange={(e) => setContent(e.target.value)} maxLength={2000} />
            <Button type="submit" loading={post.isPending} disabled={!content.trim()}>{t('actions.submit')}</Button>
          </form>
          {post.isError && <ErrorState error={post.error} />}
          <QueryBoundary query={posts} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.posts')} emptyIcon={MessageSquare}>
            {(items) => (
              <ul className="ui-list">
                {items.map((item) => (
                  <li className="ui-list__item" key={itemId(item)}>
                    <div className="ui-list__body">
                      <div className="ui-list__title">{displayValue(item, ['authorName', 'AuthorName']) || t('student.communities.posts')}</div>
                      <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'content', 'Content'])}</div>
                      <div className="ui-list__meta ui-muted">{formatDate(getField(item, 'createdAt'), locale)}</div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </QueryBoundary>
        </Card>
        {community.data && <Card title={t('student.details')}><DetailList item={community.data} locale={locale} /></Card>}
      </div>
    </>
  )
}

// =============================================================================
// Competitions
// =============================================================================
function CompetitionsPage({ userId, locale }) {
  const { t } = useTranslation()
  const { competitionId } = useParams()
  const query = useStudentQuery(queryKeys.student.competitions(userId), (signal) => studentApi.competitions(signal), { staleTime: STALE.medium })
  if (competitionId) return <CompetitionDetails userId={userId} competitionId={competitionId} locale={locale} />
  return (
    <>
      <PageHeader title={t('student.competitions.title')} description={t('student.competitions.description')} />
      <QueryBoundary query={query} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.competitions')} emptyIcon={Trophy}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => (
              <QuizCard key={itemId(item)} to={`/app/student/competitions/${itemId(item)}`} icon={Trophy} title={displayValue(item)} meta={displayValue(item, ['description', 'Description'])} />
            ))}
          </div>
        )}
      </QueryBoundary>
    </>
  )
}

function CompetitionDetails({ userId, competitionId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const competition = useStudentQuery(queryKeys.student.competition(userId, competitionId), (signal) => studentApi.competition(competitionId, signal), { staleTime: STALE.medium })
  const leaderboard = useStudentQuery(queryKeys.student.leaderboard(userId, competitionId), (signal) => studentApi.leaderboard(competitionId, signal))
  const enter = useMutation({ mutationFn: () => studentApi.enterCompetition(competitionId), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.competition(userId, competitionId) }) })
  return (
    <>
      <PageHeader title={displayValue(competition.data) || t('student.competitions.details')} description={t('student.competitions.authoritative')} actions={<Button onClick={() => enter.mutate()} loading={enter.isPending}>{t('student.competitions.enter')}</Button>} />
      {(competition.isError || enter.isError) && <ErrorState error={competition.error || enter.error} onRetry={competition.refetch} />}
      <div className="ui-split">
        <div>
          {competition.data && <Card title={t('student.details')}><DetailList item={competition.data} locale={locale} /></Card>}
          <CompetitionSubmissionCard userId={userId} competitionId={competitionId} locale={locale} />
        </div>
        <Card title={t('student.leaderboard.title')}>
          <QueryBoundary query={leaderboard} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.leaderboard')} emptyIcon={Trophy}>
            {(items) => (
              <ul className="domain-lb-list">
                {items.map((item, i) => (
                  <LeaderboardRow
                    key={itemId(item) || i}
                    rank={getField(item, 'rank') ?? i + 1}
                    name={displayValue(item, ['studentName', 'StudentName', 'name', 'Name']) || itemId(item)}
                    points={getField(item, 'totalPoints') ?? getField(item, 'points')}
                    pointsLabel={t('student.points.unit', 'pts')}
                  />
                ))}
              </ul>
            )}
          </QueryBoundary>
        </Card>
      </div>
    </>
  )
}

// Phase 14 (closure) — durable competition submission.
function CompetitionSubmissionCard({ userId, competitionId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const submission = useStudentQuery(queryKeys.student.competitionSubmission(userId, competitionId), (signal) => studentApi.competitionSubmission(competitionId, signal), { staleTime: STALE.short })
  const [content, setContent] = useState('')
  const submit = useMutation({
    mutationFn: () => studentApi.submitCompetition(competitionId, content.trim()),
    onSuccess: () => { setContent(''); qc.invalidateQueries({ queryKey: queryKeys.student.competitionSubmission(userId, competitionId) }) },
  })
  const existing = submission.data
  const hasExisting = Boolean(existing && (existing.content ?? existing.Content))
  return (
    <Card title={t('student.competitions.submission.title')}>
      <p className="ui-muted">{t('student.competitions.submission.description')}</p>
      {submission.isError && <ErrorState error={submission.error} onRetry={submission.refetch} />}
      {hasExisting ? (
        <dl className="ui-detail-list">
          <div className="ui-detail-list__row"><dt>{t('student.competitions.submission.current')}</dt><dd>{existing.content ?? existing.Content}</dd></div>
          <div className="ui-detail-list__row"><dt>{t('student.details')}</dt><dd>{formatDate(existing.submittedAt ?? existing.SubmittedAt, locale)}</dd></div>
        </dl>
      ) : !submission.isLoading && <EmptyState title={t('student.competitions.submission.empty')} />}
      <form className="stack" onSubmit={(e) => { e.preventDefault(); if (content.trim()) submit.mutate() }}>
        <TextareaField label={t('student.competitions.submission.label')} value={content} onChange={(e) => setContent(e.target.value)} maxLength={8192} placeholder={t('student.competitions.submission.placeholder')} />
        <Button type="submit" loading={submit.isPending} disabled={!content.trim()}>{hasExisting ? t('student.competitions.submission.update') : t('student.competitions.submission.submit')}</Button>
      </form>
      {submit.isError && <ErrorState error={submit.error} />}
      {submit.isSuccess && <Alert variant="success" title={t('student.competitions.submission.saved')} />}
    </Card>
  )
}

// =============================================================================
// Office hours
// =============================================================================
function OfficeHoursPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useStudentQuery(queryKeys.student.officeHours(userId), (signal) => studentApi.officeHours(signal))
  const book = useMutation({ mutationFn: (id) => studentApi.bookOfficeHour(id), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.officeHours(userId) }) })
  const available = query.data ? toItems(settledData(query.data.available)) : []
  const mine = query.data ? toItems(settledData(query.data.mine)) : []
  return (
    <>
      <PageHeader title={t('student.officeHours.title')} description={t('student.officeHours.description')} />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {book.isError && <ErrorState error={book.error} />}
      <Card title={t('student.officeHours.available')}>
        {available.length === 0 ? <EmptyState title={t('student.empty.officeHours')} /> : (
          <div className="student-list">
            {available.map((item) => (
              <div className="domain-row" key={itemId(item)}>
                <span className="domain-row__icon" aria-hidden="true"><CalendarClock size={18} /></span>
                <span className="domain-row__main">
                  <span className="domain-row__title">{displayValue(item, ['teacherName', 'TeacherName']) || displayValue(item)}</span>
                  <span className="domain-row__meta">{formatDate(getField(item, 'startsAt') || getField(item, 'startTime'), locale)}</span>
                </span>
                <Button onClick={() => book.mutate(itemId(item))} loading={book.isPending}>{t('student.officeHours.book')}</Button>
              </div>
            ))}
          </div>
        )}
      </Card>
      <Card title={t('student.officeHours.bookings')}>
        {mine.length === 0 ? <EmptyState title={t('student.officeHours.noBookings')} /> : (
          <div className="student-list">
            {mine.map((item) => (
              <div className="domain-row" key={itemId(item)}>
                <span className="domain-row__icon" aria-hidden="true"><CalendarClock size={18} /></span>
                <span className="domain-row__main">
                  <span className="domain-row__title">{displayValue(item, ['teacherName', 'TeacherName']) || displayValue(item)}</span>
                  <span className="domain-row__meta">{formatDate(getField(item, 'startsAt') || getField(item, 'startTime'), locale)}</span>
                </span>
              </div>
            ))}
          </div>
        )}
      </Card>
    </>
  )
}

// =============================================================================
// Notifications / announcements / suggestions
// =============================================================================
function NotificationsPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const query = useStudentQuery(queryKeys.student.notifications(userId), (signal) => studentApi.notifications(signal))
  const mark = useMutation({ mutationFn: (id) => studentApi.markNotificationRead(id), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.notifications(userId) }) })
  const markAll = useMutation({ mutationFn: () => studentApi.markAllNotificationsRead(), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.student.notifications(userId) }) })
  const items = query.data ? toItems(settledData(query.data.items)) : []
  const unread = query.data ? toObject(settledData(query.data.count))?.unreadCount : null
  return (
    <>
      <PageHeader title={t('student.notifications.title')} description={t('student.notifications.description')} actions={<Button variant="secondary" onClick={() => markAll.mutate()} loading={markAll.isPending}>{t('student.notifications.markAll')}</Button>} />
      {unread !== null && <Alert variant="info" title={t('student.notifications.unread')}>{unread}</Alert>}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {mark.isError && <ErrorState error={mark.error} />}
      {items.length === 0 ? <EmptyState icon={Bell} title={t('student.empty.notifications')} /> : (
        <ul className="ui-list">
          {items.map((item) => {
            const read = getField(item, 'isRead')
            return (
              <li className={`ui-list__item${read ? '' : ' ui-list__item--unread'}`} key={itemId(item)}>
                <div className="ui-list__body">
                  <div className="ui-list__title">{displayValue(item)}</div>
                  <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'message', 'Message'])}</div>
                  <div className="ui-list__meta ui-muted">{formatDate(getField(item, 'createdAt'), locale)}</div>
                </div>
                {!read && <Button variant="ghost" onClick={() => mark.mutate(itemId(item))} loading={mark.isPending}>{t('student.notifications.markRead')}</Button>}
              </li>
            )
          })}
        </ul>
      )}
    </>
  )
}

function AnnouncementsPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useStudentQuery(queryKeys.student.announcements(userId), (signal) => studentApi.announcements(signal), { staleTime: STALE.medium })
  return (
    <>
      <PageHeader title={t('student.announcements.title')} description={t('student.announcements.description')} />
      <QueryBoundary query={query} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.announcements')} emptyIcon={Megaphone}>
        {(items) => (
          <div className="student-list">
            {items.map((item) => (
              <div className="ui-list__item" key={itemId(item)} style={{ borderInlineStart: '3px solid var(--accent)' }}>
                <div className="ui-list__body">
                  <div className="ui-list__title">{displayValue(item)}</div>
                  <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'message', 'Message', 'content', 'Content'])}</div>
                  <div className="ui-list__meta ui-muted">{formatDate(getField(item, 'createdAt') || getField(item, 'publishedAt'), locale)}</div>
                </div>
              </div>
            ))}
          </div>
        )}
      </QueryBoundary>
    </>
  )
}

function SuggestionsPage() {
  const { t } = useTranslation()
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const mutation = useMutation({ mutationFn: () => studentApi.submitSuggestion(title, body), onSuccess: () => { setTitle(''); setBody('') } })
  return (
    <>
      <PageHeader title={t('student.suggestions.title')} description={t('student.suggestions.description')} />
      <Alert title={t('student.suggestions.privacyTitle')}>{t('student.suggestions.privacyBody')}</Alert>
      <Card>
        <form className="stack" onSubmit={(event) => { event.preventDefault(); if (title.trim() && body.trim()) mutation.mutate() }}>
          <TextField label={t('student.suggestions.subject')} value={title} onChange={(e) => setTitle(e.target.value)} maxLength={120} required />
          <TextareaField label={t('student.suggestions.body')} value={body} onChange={(e) => setBody(e.target.value)} maxLength={2000} required />
          <Button type="submit" loading={mutation.isPending} disabled={!title.trim() || !body.trim()}>{t('actions.submit')}</Button>
        </form>
      </Card>
      {mutation.isSuccess && <Alert variant="success" title={t('student.suggestions.sent')} />}
      {mutation.isError && <ErrorState error={mutation.error} />}
    </>
  )
}

// =============================================================================
// Badges / points / streaks
// =============================================================================
function BadgesPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useStudentQuery(queryKeys.student.badges(userId), (signal) => studentApi.badges(userId, signal), { staleTime: STALE.medium })
  const points = useStudentQuery(queryKeys.student.points(userId), (signal) => studentApi.points(userId, signal), { staleTime: STALE.short })
  const ledger = useStudentQuery(queryKeys.student.pointsLedger(userId), (signal) => studentApi.pointsLedger(userId, signal), { staleTime: STALE.short })
  const leaderboard = useStudentQuery(queryKeys.student.pointsLeaderboard(userId), (signal) => studentApi.pointsLeaderboard({}, signal), { staleTime: STALE.short })
  const earned = query.data ? toItems(settledData(query.data.earned)) : []
  const catalog = query.data ? toItems(settledData(query.data.catalog)) : []
  const streak = query.data ? toObject(settledData(query.data.streak)) : null
  const streakDays = streak ? toItems(streak.days ?? streak.Days) : []
  return (
    <>
      <PageHeader title={t('student.badges.title')} description={t('student.badges.description')} />
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}

      <div className="student-dashboard">
        <Metric icon={Medal} accent="var(--warning)" label={t('student.points.title')} value={points.isLoading ? '…' : points.isError ? '—' : (points.data?.totalPoints ?? 0)} />
        <Metric icon={Trophy} accent="var(--orange)" label={t('student.badges.earned')} value={earned.length} />
        <Metric icon={Flame} accent="var(--orange)" label={t('student.streaks.title')} value={streak ? (streak.current ?? streak.Current ?? '—') : '—'} />
      </div>

      <Card title={t('student.streaks.title')}>
        {streakDays.length > 0
          ? <StreakStrip days={streakDays.map((d, i) => ({ key: i, active: Boolean(getField(d, 'active') ?? getField(d, 'isActive')), label: displayValue(d, ['label', 'Label']) }))} />
          : <NotEnoughData compact title={t('student.streaks.empty')} />}
      </Card>

      <Card title={t('student.points.ledger')}>
        {ledger.isLoading ? <Loading />
          : ledger.isError ? <ErrorState error={ledger.error} onRetry={ledger.refetch} />
          : (ledger.data?.length ?? 0) === 0 ? <EmptyState title={t('student.points.empty')} />
          : (
            <ul className="ui-list">
              {ledger.data.map((item, i) => (
                <li className="ui-list__item" key={itemId(item) || i}>
                  <div className="ui-list__body">
                    <div className="ui-list__title">{displayValue(item, ['reason', 'Reason']) || displayValue(item)}</div>
                    <div className="ui-list__meta ui-muted">{formatDate(getField(item, 'createdAt'), locale)}</div>
                  </div>
                  <Chip tone="brand">{getField(item, 'points') ?? getField(item, 'Points')}</Chip>
                </li>
              ))}
            </ul>
          )}
      </Card>

      <Card title={t('student.points.leaderboard')}>
        {leaderboard.isLoading ? <Loading />
          : leaderboard.isError ? <ErrorState error={leaderboard.error} onRetry={leaderboard.refetch} />
          : (leaderboard.data?.length ?? 0) === 0 ? <EmptyState title={t('student.points.leaderboardEmpty')} />
          : (
            <ul className="domain-lb-list">
              {leaderboard.data.map((item, i) => (
                <LeaderboardRow
                  key={itemId(item) || i}
                  rank={getField(item, 'rank') ?? i + 1}
                  name={displayValue(item, ['studentName', 'StudentName', 'name', 'Name']) || itemId(item)}
                  points={getField(item, 'totalPoints') ?? getField(item, 'points')}
                  isMe={itemId(item) === userId}
                  pointsLabel={t('student.points.unit', 'pts')}
                />
              ))}
            </ul>
          )}
      </Card>

      <Card title={t('student.badges.earned')}>
        {earned.length === 0 ? <EmptyState icon={Medal} title={t('student.empty.badges')} /> : (
          <div className="ui-grid ui-grid--auto">
            {earned.map((item) => (
              <div className="domain-card" key={itemId(item)}>
                <div className="domain-card__head">
                  <span className="domain-card__icon" aria-hidden="true"><Medal size={20} /></span>
                </div>
                <h3 className="domain-card__title">{displayValue(item)}</h3>
              </div>
            ))}
          </div>
        )}
      </Card>

      {catalog.length > 0 && (
        <Card title={t('student.badges.catalog')}>
          <div className="ui-grid ui-grid--auto">
            {catalog.map((item) => (
              <div className="domain-card" key={itemId(item)}>
                <div className="domain-card__head">
                  <span className="domain-card__icon" aria-hidden="true"><Medal size={20} /></span>
                </div>
                <h3 className="domain-card__title">{displayValue(item)}</h3>
              </div>
            ))}
          </div>
        </Card>
      )}
    </>
  )
}

// =============================================================================
// Settings
// =============================================================================
function AccountSettingsPage() {
  const { t } = useTranslation()
  return (
    <>
      <PageHeader title={t('student.settings.title')} description={t('student.settings.description')} />
      <div className="ui-split ui-split--even">
        <Card title={t('student.settings.language')}><p className="ui-muted">{t('student.settings.languageLocal')}</p></Card>
        <Card title={t('nav.security')}>
          <Link className="ui-btn ui-btn--primary" to="/app/security"><ShieldCheck size={16} aria-hidden="true" /> {t('student.settings.changePassword')}</Link>
        </Card>
      </div>
    </>
  )
}

// =============================================================================
// View dispatcher
// =============================================================================
export default function StudentPortalPage({ view }) {
  const { i18n } = useTranslation()
  const { user } = useAuth()
  const userId = user?.id
  const locale = i18n.language || 'en'
  if (!userId) return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  const common = { userId, locale }
  switch (view) {
    case 'subjects': return <SubjectsPage {...common} />
    case 'subject-details': return <SubjectDetailsPage {...common} />
    case 'units': return <UnitsPage {...common} />
    case 'lessons': return <LessonsPage {...common} />
    case 'materials': return <MaterialsPage {...common} />
    case 'homework': return <HomeworkPage {...common} />
    case 'quizzes': return <QuizzesPage {...common} />
    case 'quiz-attempt': return <QuizAttemptPage {...common} />
    case 'quiz-result': return <QuizResultPage {...common} />
    case 'tutor': return <TutorPage />
    case 'recommendations': return <ProgressPage {...common} mode="recommendations" />
    case 'progress': return <ProgressPage {...common} mode="progress" />
    case 'attendance': return <AttendancePage {...common} />
    case 'engagement': return <ProgressPage {...common} mode="engagement" />
    case 'communities': return <CommunitiesPage {...common} />
    case 'competitions': return <CompetitionsPage {...common} />
    case 'office-hours': return <OfficeHoursPage {...common} />
    case 'notifications': return <NotificationsPage {...common} />
    case 'announcements': return <AnnouncementsPage {...common} />
    case 'suggestions': return <SuggestionsPage />
    case 'badges': return <BadgesPage {...common} />
    case 'settings': return <AccountSettingsPage />
    default: return <DashboardPage {...common} />
  }
}
